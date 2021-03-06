﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq.Expressions;
using System.Text;

namespace Gekko.Parser.Gek
{

    public class GekkoSB
    {
        public StringBuilder storage = null;

        public GekkoSB()
        {
            this.storage = new StringBuilder();
        }

        public override string ToString()
        {
            return this.storage.ToString();
        }

        public void A(string s)
        {
            this.storage.Append(s);
        }

        public void CA(string s)
        {
            this.storage.Clear();
            this.storage.Append(s);
        }
        
    }

    //public class GekkoStringBuilder
    //{
    //    public StringBuilder sb = null;
    //    public GekkoStringBuilder()
    //    {
    //        this.sb = new StringBuilder();
    //    }
        
    //    public void __(GekkoStringBuilder sb2)
    //    {
    //        this.sb.Append(sb2);
    //    }

    //    public void __(string s)
    //    {
    //        this.sb.Append(s);
    //    }

    //    public void __Line(GekkoStringBuilder sb2)
    //    {
    //        this.sb.Append(sb2);
    //        this.sb.AppendLine();
    //    }

    //    public void __Line(string s)
    //    {
    //        this.sb.AppendLine(s);
    //    }

    //    public void __Clear(GekkoStringBuilder sb2)
    //    {
    //        this.sb.Clear();
    //        this.sb.Append(sb2);
    //    }
        
    //    public void __Clear(string s)
    //    {
    //        this.sb.Clear();
    //        this.sb.Append(s);
    //    }
    //}
    
    public enum EOptionType
    {
        String,
        Val,
        Date
    }               
    
    public class ParserGekWalkASTAndEmit
    {

        public static readonly GekkoTime tNULL = new GekkoTime(EFreq.Annual, -12345, 1);

        
        
        public enum ELastCommand
        {
            Unknown,
            Genr,
            Val,
            String,
            Date
        }

        public static void WalkASTAndEmit(ASTNode node, int absoluteDepth, int relativeDepth, string textInput, W w, P p)
        {
            //OLDSTUFF_DELETE();

            //if (node.Text != null)
            //{                
            //    //this is done in CreateAstNodes...
            //    //node.Text = G.ReplaceGlueNew(node.Text);
            //}    

            ////if (absoluteDepth == 76)
            //{
            //    G.Writeln("---> " + absoluteDepth + " " + relativeDepth);
            //}

            if (node.Parent != null)
            {
                string s = null;
                if (node.Text == "ASTTUPLEITEM")
                {
                    s = "_tuple_" + node.Number;  //a (VAL x, GENR y) = ... tuple is kind of like two separate commands.
                }
                node.commandLinesCounter = node.Parent.commandLinesCounter + s;  //default, may be overridden if new command is encountered.               

            }

            //TODO: IF etc.
            if (node.Text == "ASTFORSTATEMENTS" || node.Text == "ASTIFSTATEMENTS" || node.Text == "ASTFUNCTIONDEFCODE")
            {
                relativeDepth = 0;
            }
            
            if (relativeDepth == 1)
            {
                //NEW COMMAND encountered
                //Corresponds to a new statement at current "indentation" level
                w.wh = new WalkHelper();
                w.wh.currentCommand = node.Text;  //if for instance a GENR statement, this field will be 'ASTGENR'
                w.commandLinesCounter++;  //becomes 0 first time here (starts at -1)
                node.commandLinesCounter = w.commandLinesCounter.ToString(); //used for the O(node)-method, so that o0, o1, o2 numbers do not suddenly change after for instance a FOR.
                w.expressionCounter = -1;  //for labels in PRT elements
            }

            if (node.Text == "ASTGENR" || node.Text == "ASTGENRLHSFUNCTION" || node.Text == "ASTPRTELEMENT" || node.Text == "ASTOLSELEMENT" || node.Text == "ASTTABLESETVALUESELEMENT")
            {
                //This local cache is only used for commands that do implicit timeseries looping with expressions
                //For instance PRT fX%i (PRT fXnz would end in global cache), where we do not have to 
                //  find fx%i for each period in the time loop (the reference fx%i is always fixed over that loop
                //  which is internal/implicit inside the GENR statement).
                ClearLocalStatementCache(w);
            }

            //Before sub-nodes
            switch (node.Text)
            {                
                case "ASTFUNCTIONDEF":
                    {
                        if (w.functionHelper != null)
                        {
                            G.Writeln2("*** ERROR: Function definition inside function definition not allowed");
                            throw new GekkoException();
                        }
                        if (absoluteDepth > 1)
                        {
                            G.Writeln2("*** ERROR: Function definition cannot be inside loop, IF-statement etc. Place at top or end of file.");
                            throw new GekkoException();
                        }
                        w.functionHelper = new FunctionArgumentsHelper();
                    }
                    break;
            }
            
            foreach (ASTNode child in node.ChildrenIterator())
            {
                WalkASTAndEmit(child, absoluteDepth + 1, relativeDepth + 1, textInput, w, p);
                //return; Globals.testing = true;
            }            

            //In general, we have this pattern
            //
            //   ASTCOPY (or other node)
            //     ASTOPT_
            //       ASTOPT_STRING_AS
            //       ASTOPT_DATE_END
            //       ASTOPT_DATE_START
            //   
            //The code below is to avoid a lot of repetitive coding regarding options to commands
            //#astopt
            if (node.Text != null && node.Text.StartsWith("ASTOPT_STRING_"))
            {
                string s2 = node.Text.Substring(14);
                if (node.ChildrenCount() == 0) node.Code += "o" + Num(node) + ".opt_" + s2.ToLower() + " = `yes`;" + G.NL;  //Using PRT<rows> instead of the more explicit PRT<rows=yes>.
                else
                {
                    node.Code += "o" + Num(node) + ".opt_" + s2.ToLower() + " = O.GetString(" + node[0].Code + ");" + G.NL;
                }
            }
            else if (node.Text != null && node.Text.StartsWith("ASTOPT_VAL_"))
            {
                string s2 = node.Text.Substring(11);                
                if (node.ChildrenCount() == 0) throw new GekkoException();
                else node.Code += "o" + Num(node) + ".opt_" + s2.ToLower() + " = O.GetVal(" + node[0].Code + ", t);" + G.NL;
            }
            else if (node.Text != null && node.Text.StartsWith("ASTOPT_DATE_"))
            {
                string s2 = node.Text.Substring(12);                
                if (node.ChildrenCount() == 0) throw new GekkoException();
                else node.Code += "o" + Num(node) + ".opt_" + s2.ToLower() + " = O.GetDate(" + node[0].Code + ");" + G.NL;
            }
            else
            {
                //After sub-nodes
                switch (node.Text)
                {
                    case "+":
                        {
                            node.Code = "O.Add(" + node[0].Code + ", " + node[1].Code + ", t)";
                        }
                        break;                    
                    case "-":
                        {
                            node.Code = "O.Subtract(" + node[0].Code + ", " + node[1].Code + ", t)";
                        }
                        break;
                    case "*":
                        {
                            node.Code = "O.Multiply(" + node[0].Code + ", " + node[1].Code + ", t)";
                        }
                        break;
                    case "/":
                        {
                            node.Code = "O.Divide(" + node[0].Code + ", " + node[1].Code + ", t)";
                        }
                        break;
                    case "ASTPOW":
                        {
                            node.Code = "O.Power(" + node[0].Code + ", " + node[1].Code + ", t)";
                        }
                        break;
                    case "&+":
                        {
                            node.Code = "O.AndAdd(" + node[0].Code + ", " + node[1].Code + ", t)";
                        }
                        break;
                    case "&-":
                        {
                            node.Code = "O.AndSubtract(" + node[0].Code + ", " + node[1].Code + ", t)";
                        }
                        break;
                    case "&*":
                        {
                            node.Code = "O.AndMultiply(" + node[0].Code + ", " + node[1].Code + ", t)";
                        }
                        break;
                    case "[":  //indexer
                    case Globals.symbolGlueChar6:  //indexer, '[_['
                    case Globals.symbolGlueChar7:  //indexer, '[¨['
                        {
                            //At the moment only 1 dimension supported                        
                            if (node.ChildrenCount() > 3) throw new GekkoException();
                            if (node[1].Text == "ASTINDEXERELEMENTPLUS")
                            {
                                node.Code += "O.IndexerPlus(" + node[0].Code + ", " + node[1].Code + ", t)";
                            }
                            else
                            {
                                if (node.ChildrenCount() == 2)
                                {
                                    node.Code += "O.Indexer(" + node[0].Code + ", " + node[1].Code + ", t)";
                                }
                                else
                                {
                                    node.Code += "O.Indexer(" + node[0].Code + ", " + node[1].Code + ", " + node[2].Code + ", t)";
                                }
                            }                            
                        }
                        break;
                    case "ASTAPPEND":
                        {
                            node.Code = "new ScalarString(`append`)";
                        }
                        break;                    
                    case "ASTAT":
                        {
                            node.Code = "new ScalarString(`@`)";
                        }
                        break;
                    case "ASTSTAR":
                        {
                            node.Code = "new ScalarVal(123454321)";  //signifies infinity for REP * in UPD.
                        }
                        break;
                    case "ASTUPDDATACOMPLICATED":
                        {
                            
                            GetCodeFromAllChildren(node);
                        }
                        break;
                    case "ASTS":
                    case "ASTSN":
                        {
                            node.Code += AddPrintCode("s", node[0].Code, node.Parent.Parent.Text, node);
                        }
                        break;
                    case "ASTBANK":
                        {                            
                            if (node.ChildrenCount() == 0)
                            {
                                node.Code = "new ScalarString(`" + Globals.primaryCheatString + "`)";
                            }
                            else node.Code = node[0].Code;
                        }
                        break;
                    case "ASTSD":
                        {
                            node.Code += AddPrintCode("sd", node[0].Code, node.Parent.Parent.Text, node);
                        }
                        break;
                    case "ASTSDP":
                        {
                            node.Code += AddPrintCode("sdp", node[0].Code, node.Parent.Parent.Text, node);
                        }
                        break;
                    case "ASTSP":
                        {
                            node.Code += AddPrintCode("sp", node[0].Code, node.Parent.Parent.Text, node);
                        }
                        break;
                    case "ASTPRTROWS":
                        {
                            GetCodeFromAllChildren(node);
                        }
                        break;                    
                    case "ASTCLEAR":
                        {
                            node.Code += "O.Clear o" + Num(node) + " = new O.Clear();" + G.NL;
                            node.Code += "o" + Num(node) + ".p = p;";
                            if (node[0].ChildrenCount() > 0)
                            {
                                node.Code += "o" + Num(node) + ".name = `" + node[0][0].Text + "`;" + G.NL;
                            }
                            GetCodeFromAllChildren(node);
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTCLONE":
                        {
                            node.Code += "O.Clone o" + Num(node) + " = new O.Clone();" + G.NL;                                                        
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTCLEAR2":
                        {
                            node.Code += "O.Clear2 o" + Num(node) + " = new O.Clear2();" + G.NL;
                            node.Code += "o" + Num(node) + ".p = p;";
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTCLOSE":
                        {                            
                            node.Code += "O.Close o" + Num(node) + " = new O.Close();" + G.NL;
                            node.Code += "o" + Num(node) + ".name = `" + node[0].Text + "`;" + G.NL;
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTRESET":
                        {
                            node.Code += "O.Reset o" + Num(node) + " = new O.Reset();" + G.NL;
                            node.Code += "o" + Num(node) + ".p = p;";
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTRESTART":
                        {
                            node.Code += "O.Restart o" + Num(node) + " = new O.Restart();" + G.NL;
                            node.Code += "o" + Num(node) + ".p = p;";
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;

                    case "ASTCLOSESTAR":
                        {                                     
                            node.Code += "O.Close o" + Num(node) + " = new O.Close();" + G.NL;
                            node.Code += "o" + Num(node) + ".name = `*`;" + G.NL;
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;                       
                        }
                        break;
                    case "ASTCLS":
                        {
                            node.Code += "Program.Cls(``);"; //main window
                            node.Code += "Program.Cls(`output`);";  //output window
                        }
                        break;
                    case "ASTCOLLAPSE":
                        {
                            string type = "null";
                            if (node.ChildrenCount() >= 3) type = "`" + node[2].Text + "`";
                            node.Code += "Program.Collapse(O.GetString(" + node[0].Code + "), O.GetString(" + node[1].Code + "), " + type + ");" + G.NL;
                        }
                        break;
                    //case "ASTCOPY":
                    //    {
                    //        node.Code += "O.Copy o" + Num(node) + " = new O.Copy();" + G.NL;                                                        
                    //        node.Code += node[0].Code;  //options
                    //        node.Code += "o" + Num(node) + ".listItems = new List<string>();" + G.NL;
                    //        node.Code += node[1].Code;  //list1
                    //        node.Code += "o" + Num(node) + ".listItems1 = o" + Num(node) + ".listItems;" + G.NL;
                    //        if (node.ChildrenCount() > 2)
                    //        {
                    //            node.Code += "o" + Num(node) + ".listItems = new List<string>();" + G.NL;
                    //            node.Code += node[2].Code;  //list2
                    //            node.Code += "o" + Num(node) + ".listItems2 = o" + Num(node) + ".listItems;" + G.NL;
                    //        }
                    //        node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                    //    }
                    //    break;

                    case "ASTCOPY":
                        {
                            node.Code += "O.Copy o" + Num(node) + " = new O.Copy();" + G.NL;
                            GetCodeFromAllChildren(node);
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTDELETE":
                        {
                            node.Code += "O.Delete o" + Num(node) + " = new O.Delete();" + G.NL;
                            node.Code += node[0].Code;
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                            break;
                        }
                    case "ASTDELETEALL":
                        {
                            node.Code += "O.Delete o" + Num(node) + " = new O.Delete();" + G.NL;
                            node.Code += "o" + Num(node) + ".all = true;" + G.NL;
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                            break;
                        }
                    case "ASTCREATE":
                        {
                            node.Code += "O.Create o" + Num(node) + " = new O.Create();" + G.NL;
                            node.Code += node[0].Code;
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                            break;
                        }
                    case "ASTCREATEEXPRESSION":
                        {
                            node.Code += "O.CreateExpression o" + Num(node) + " = new O.CreateExpression();" + G.NL;
                            node.Code += "o" + Num(node) + ".lhs = " + node[0].Code + ";" + G.NL;
                            node.Code += "o" + Num(node) + ".rhs = " + node[1].Code + ";" + G.NL;
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                            break;
                        }
                    case "ASTENDO":
                        {
                            node.Code += "O.Endo o" + Num(node) + " = new O.Endo();" + G.NL;
                            if (node.ChildrenCount() > 0) node.Code += node[0].Code;
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTENDOQUESTION":
                        {
                            node.Code += "O.Endo o" + Num(node) + " = new O.Endo();" + G.NL;
                            node.Code += "o" + Num(node) + ".question = true;" + G.NL;
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTEXO":
                        {
                            node.Code += "O.Exo o" + Num(node) + " = new O.Exo();" + G.NL;
                            if (node.ChildrenCount() > 0) node.Code += node[0].Code;
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTEXOQUESTION":
                        {
                            node.Code += "O.Exo o" + Num(node) + " = new O.Exo();" + G.NL;
                            node.Code += "o" + Num(node) + ".question = true;" + G.NL;
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTEXIT":  // <command>
                        {
                            node.Code += "Program.Exit();" + G.NL;
                            //#9807235423 return problem
                            //node.Code += "return true;" + G.NL;  //probably superfluous
                            node.Code += "return;" + G.NL;  //probably superfluous
                        }
                        break;

                    case "ASTSTOP":
                        {
                            node.Code += "Program.Stop(p);" + G.NL;
                            //#9807235423 return problem
                            //node.Code += "return true;" + G.NL;  //probably superfluous
                            node.Code += "return;" + G.NL;  //probably superfluous
                        }
                        break;

                    case "ASTREPLACE":
                        {

                        }
                        break;
                    case "ASTFINDMISSINGDATA": // <command> <period>
                        {
                            node.Code += "O.Findmissingdata o" + Num(node) + " = new O.Findmissingdata();" + G.NL;
                            GetCodeFromAllChildren(node);
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;                            
                        }
                        break;
                    case "ASTHDG":
                        node.Code += "Program.Hdg(O.GetString(" + node[0].Code + "));";
                        break;
                    case "ASTTELL":
                        {
                            string s = "false";
                            if (node.ChildrenCount() > 1)
                            {
                                s = "true";
                            }
                            node.Code += "Program.Tell(O.GetString(" + node[0].Code + "), " + s + ");";                            
                        }
                        break;
                    case "ASTSYS":
                        {
                            if (node.ChildrenCount() == 0)
                            {
                                node.Code += "Program.Sys(null);";
                            }
                            else
                            {
                                node.Code += "Program.Sys(O.GetString(" + node[0].Code + "));";
                            }
                        }
                        break;
                    case "ASTHELP":
                        {
                            string txt = "null";
                            if (node.ChildrenCount() > 0) txt = "`" + node[0].Text + "`";
                            node.Code += "Program.Help(" + txt + ");" + G.NL;
                        }
                        break;
                    case "ASTCREATEQUESTION":
                        {
                            node.Code += "O.Create o" + Num(node) + " = new O.Create();" + G.NL;
                            node.Code += "o" + Num(node) + ".question = true;" + G.NL;
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                            break;
                        }
                    case "ASTCHECKOFF":
                        {
                            node.Code += "O.Checkoff o" + Num(node) + " = new O.Checkoff();" + G.NL;
                            if (node.ChildrenCount() == 0)
                            {
                                node.Code += "o" + Num(node) + ".type = `clear`;";
                            }
                            else if (node.ChildrenCount() == 1 && node[0].Text == "?")
                            {
                                node.Code += "o" + Num(node) + ".type = `?`;";
                            }
                            else
                            {
                                node.Code += node[0].Code;
                            }
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                            break;
                        }
                    case "ASTCURLY":
                        {
                            node.Code = node[0].Code;
                        }
                        break;
                    case "ASTCURLYSIMPLE":
                        {                            
                            node[0].Code = null;  //clearing this
                            HandleScalar(node[0], true, w);
                            node.Code = node[0].Code;                         
                        }
                        break;
                    case "ASTEXPRESSION":
                        {
                            node.Code = node[0].Code;
                        }
                        break;
                    case "ASTD":
                        {
                            node.Code += AddPrintCode("d", node[0].Code, node.Parent.Parent.Text, node);
                        }
                        break;
                    case "ASTDP":
                        {
                            node.Code += AddPrintCode("dp", node[0].Code, node.Parent.Parent.Text, node);
                        }
                        break;
                    case "ASTDATE":
                        {
                            if (node[0].Text == "?")
                            {
                                if (node.ChildrenCount() > 1)
                                {
                                    node.Code += "O.Date.Q(`" + node[1].Text + "`);" + G.NL;
                                }
                                else
                                {
                                    node.Code += "Program.Mem(`date`);" + G.NL;
                                }
                            }
                            else
                            {
                                string nodeCode = HandleDate(node, node[1].Code);
                                node.Code += nodeCode;
                            }
                        }
                        break;
                    case "ASTDATE2":
                        {
                            //TODO:
                            //TODO:
                            //TODO: FromStringToDate() depends upon current freq -- fix that!
                            //TODO:
                            //TODO:
                            node.Code += "new ScalarDate(G.FromStringToDate(`" + node[0].Text + "`))";
                        }
                        break;
                    case "ASTDATES":
                        {
                            //what about G.GetStart/EndDate()??
                            //We use  O.GetDateChoices.FlexibleStart and  O.GetDateChoices.FlexibleEnd
                            //These can transform an integer into a quarterly or monthly frequency.
                            //This is only legal/possible for two such dates (from/to).
                            string ss1 = node.GetChildCode(0);
                            string s1 = null;
                            if (ss1 == null) s1 = "Globals.globalPeriodStart";
                            else s1 = "O.GetDate(" + ss1 + ", O.GetDateChoices.FlexibleStart);" + G.NL;
                            string ss2 = node.GetChildCode(1);
                            string s2 = null;
                            if (ss2 == null) s2 = "Globals.globalPeriodEnd";
                            else s2 = "O.GetDate(" + ss2 + ", O.GetDateChoices.FlexibleEnd);" + G.NL;
                            node.Code += "o" + Num(node) + ".t1 = " + s1 + ";" + G.NL;
                            node.Code += "o" + Num(node) + ".t2 = " + s2 + ";" + G.NL;
                        }
                        break;
                    case "ASTDOTINDEXER":
                        {

                        }
                        break;
                    case "ASTDOUBLENEGATIVE":
                        {
                            node.Code += node[0].Code;
                        }
                        break;
                    case "ASTDOUBLE":
                        {
                            //TODO TODO TODO
                            //TODO TODO TODO
                            //TODO TODO TODO find out about GLUEDOT etc. --> how to replace it.
                            //TODO TODO TODO
                            //TODO TODO TODO

                            //TODO TODO TODO
                            //TODO TODO TODO
                            //TODO TODO TODO use the cache to avoid repetitions of numbers
                            //TODO TODO TODO
                            //TODO TODO TODO

                            string minus = HandleNegate(node);
                            string doubleWithNumber = "d" + ++Globals.counter;
                            string s = "new ScalarVal(" + minus + node[0].Text.Replace(Globals.symbolGlueChar3, "") + "d)";
                            w.headerCs.AppendLine("public static readonly ScalarVal " + doubleWithNumber + " = " + s + ";");
                            node.Code = doubleWithNumber; //no need for checking if it exists                      
                        }
                        break;
                    case "ASTIF":
                        {                            
                            node.Code += "if(" + node[0].Code + ") {";                            
                            node.Code += node[1].Code;
                            node.Code += "}";
                            if (node[2] != null)
                            {                                
                                node.Code += "else {";                             
                                node.Code += node[2].Code;
                                node.Code += "}";
                            }                            
                        }
                        break;
                    case "or": //casing unknown for OR
                    case "oR":
                    case "Or":
                    case "OR":
                        {                            
                            node.Code += "((";
                            node.Code += node[0].Code;
                            node.Code += ") || (";
                            node.Code += node[1].Code;
                            node.Code += "))";
                        }
                        break;
                    case "and": //casing unknown for AND
                    case "anD":
                    case "aNd":
                    case "aND":
                    case "And":
                    case "AnD":
                    case "ANd":
                    case "AND":                    
                        {
                            node.Code += "((";
                            node.Code += node[0].Code;
                            node.Code += ") && (";
                            node.Code += node[1].Code;
                            node.Code += "))";
                        }
                        break;
                    case "not": //casing unknown for NOT
                    case "noT":
                    case "nOt":
                    case "nOT":
                    case "Not":
                    case "NoT":
                    case "NOt":
                    case "NOT":
                        {
                            node.Code += "!(";
                            node.Code += node[0].Code;                            
                            node.Code += ")";
                        }
                        break;
                    case "ASTCOMPARE":
                        {
                            string op = node[0][0].Text;
                            string code1 = node[1].Code;
                            string code2 = node[2].Code;
                            if (op == "ASTIFOPERATOR4")  //"<"
                            {                                
                                node.Code += "O.StrictlySmallerThan(" + code1 + "," + code2 + ", t)";
                            }
                            else if (op == "ASTIFOPERATOR6")  //"<="
                            {
                                node.Code += "O.SmallerThanOrEqual(" + code1 + "," + code2 + ", t)";
                            }
                            else if (op == "ASTIFOPERATOR1") //"=="
                            {
                                node.Code += "O.Equals(" + code1 + "," + code2 + ", t)";
                            }
                            else if (op == "ASTIFOPERATOR5")  //">="
                            {
                                node.Code += "O.LargerThanOrEqual(" + code1 + "," + code2 + ", t)";
                            }
                            else if (op == "ASTIFOPERATOR3") //">"
                            {
                                node.Code += "O.StrictlyLargerThan(" + code1 + "," + code2 + ", t)";
                            }
                            else if (op == "ASTIFOPERATOR2") //"<>"
                            {
                                node.Code += "!O.Equals(" + code1 + "," + code2 + ", t)";
                            }
                        }
                        break;
                    case "ASTFORDATE":
                        {
                            if (node[0].Text != "ASTFORLEFTSIDE" || node[1].Text != "ASTFORRIGHTSIDE" || node[2].Text != "ASTFORSTATEMENTS")
                            {
                                throw new GekkoException();
                            }
                            string nameSimpleIdent = node[0][0].nameSimpleIdent;
                            if (nameSimpleIdent == null)
                            {
                                G.Writeln2("*** ERROR: Composed names not (yet) allowed for loop variables");
                                throw new GekkoException();
                            }
                            string codeFrom = node[1][0].Code;
                            string codeEnd = node[1][1].Code;
                            string codeStep = null;
                            ASTNode stepNode = node[1][2];
                            if (stepNode == null) codeStep = "new ScalarVal(1d)";
                            else codeStep = node[1][2].Code;
                            string statements = node[2].Code;  //has been done in "ASTFORSTATEMENTS"
                            string tempName = "temp" + ++Globals.counter;
                            string startName = "start" + ++Globals.counter;
                            string endName = "end" + ++Globals.counter;
                            string stepName = "step" + ++Globals.counter;
                            //NOTE this will mean that the end and step are fixed when seeing the FOR. Should be ok. Alternative is crazy.                            

                            string loopVariable = null;
                            string setLoopStringCs = CacheRefScalarCs(out loopVariable, nameSimpleIdent, w.scalarCache, w.headerCs, EScalarRefType.Date, "Globals.tNull", false, true, false);
                            node.Code += setLoopStringCs + G.NL;                            
                            node.Code += "int " + stepName + " = O.GetInt(" + codeStep + ");" + G.NL;
                            node.Code += "GekkoTime " + startName + " = O.GetDate(" + codeFrom + ");" + G.NL;
                            node.Code += "GekkoTime " + endName + " = O.GetDate(" + codeEnd + ");" + G.NL;  //added a tiny bit of steplength, to guard against rounding errors
                            node.Code += "ScalarDate " + tempName + " = (ScalarDate)" + loopVariable + ";" + G.NL;
                            node.Code += "try {";
                            node.Code += "for (" + tempName + ".date = " + startName + " ; O.ContinueIterating(" + tempName + ".date, " + endName + ", " + stepName + "); " + tempName + ".date = O.GetDate(" + tempName + ".Add(" + codeStep + ", t)))";
                            //node.Code += "while (O.CompareDates("+tempName+", "+endName+", " gn(`__d" + i2 + "`), Assigns.GetDouble(" + codeStep + ") > 0d))";
                            node.Code += "{" + G.NL;
                            node.Code += statements;
                            node.Code += "}" + G.NL;
                            node.Code += "} //end of try" + G.NL;  //assign var is always removed, also in case of error
                            node.Code += "finally {" + G.NL;
                            node.Code += "O.RemoveScalar(`" + nameSimpleIdent + "`);" + G.NL;
                            node.Code += "}  //end of finally" + G.NL;  //end of finally                            
                        }
                        break;
                    case "ASTFORNAME":
                    case "ASTFORSTRING":
                        {
                            bool x = false;
                            if (node.Text == "ASTFORNAME") x = true;
                            if (node[0].Text != "ASTFORLEFTSIDE2" || node[1].Text != "ASTFORRIGHTSIDE2" || node[2].Text != "ASTFORSTATEMENTS")
                            {
                                throw new GekkoException();
                            }

                            int n0 = node[0].ChildrenCount();
                            int n1 = node[1].ChildrenCount();
                            if (n0 != n1) throw new GekkoException();  //is not possible anyway

                            if (n0 == 1)
                            {
                                //Normal string loop
                                string nameSimpleIdent = node[0][0].nameSimpleIdent;
                                if (nameSimpleIdent == null)
                                {
                                    G.Writeln2("*** ERROR: Composed names not allowed for loop variables");
                                    throw new GekkoException();
                                }
                                node.Code += "O.ForString o" + Num(node) + " = new O.ForString();" + G.NL;
                                string rightSide = node[1][0].Code;
                                node.Code += rightSide;
                                string statements = node[2].Code;  //has been done in "ASTFORSTATEMENTS"                                                                                                                
                                //string iName = "i" + ++Globals.counter;
                                string tempName = "temp" + ++Globals.counter;
                                node.Code += "try {";
                                node.Code += "foreach(string " + tempName + " in o" + Num(node) + ".listItems) {" + G.NL;
                                string loopVariable = null; //The line below emits "O.SetValFromCache(..., tempName)", same as a "STRING x = ..." statement



                                string setLoopStringCs = CacheRefScalarCs(out loopVariable, nameSimpleIdent, w.scalarCache, w.headerCs, EScalarRefType.String, tempName, x, true, false);
                                node.Code += setLoopStringCs + G.NL;
                                node.Code += statements;
                                node.Code += "}" + G.NL;
                                node.Code += "} //end of try" + G.NL;  //assign var is always removed, also in case of error
                                node.Code += "finally {" + G.NL;
                                //node.Code += loopVariable + " = null;" + G.NL; //invalidates pointer
                                node.Code += "O.RemoveScalar(`" + nameSimpleIdent + "`);" + G.NL;
                                node.Code += "}  //end of finally" + G.NL;  //end of finally
                            }
                            else
                            {
                                //Parallel string loop
                                node.Code += "O.ForString o" + Num(node) + " = new O.ForString();" + G.NL;
                                for (int i = 0; i < n0; i++)
                                {                                    
                                    if (node[0][i].nameSimpleIdent == null)
                                    {
                                        G.Writeln2("*** ERROR: Composed names not allowed for loop variables");
                                        throw new GekkoException();
                                    }
                                    //node.Code += "string s" + Num(node) + "_" + i + " = `" + node[0][i].nameSimpleIdent + "`;" + G.NL;
                                }
                                string nme = "test" + ++Globals.counter;
                                string nme2 = "test" + ++Globals.counter;
                                string nme3 = "test" + ++Globals.counter;
                                
                                node.Code += "List<List<string>> " + nme + " = new List<List<string>>();" + G.NL;
                                node.Code += "List<string> " + nme2 + " = new List<string>();" + G.NL;
                                for (int i = 0; i < n0; i++)
                                {
                                    node.Code += node[1][i].Code;
                                    node.Code += "List<string> x" + Num(node) + "_" + i + " = o" + Num(node) + ".listItems;" + G.NL;
                                    node.Code += nme + ".Add(x" + Num(node) + "_" + i + ");" + G.NL;
                                    node.Code += nme2 + ".Add(`" + node[0][i].nameSimpleIdent + "`);" + G.NL;
                                }                                

                                string statements = node[2].Code;  //has been done in "ASTFORSTATEMENTS"                                                                                                                                                
                                string tempName = "temp" + ++Globals.counter;
                                node.Code += "try {" + G.NL;
                                //node.Code += "foreach(string " + tempName + " in o" + Num(node) + ".listItems) {" + G.NL;
                                                                
                                node.Code += "int " + nme3 + "= O.ForListMax(" + nme + ");" + G.NL;
                                node.Code += "O.ForListCheck(" + nme2 + ");" + G.NL;

                                node.Code += "for (int i = 0; i < " + nme3 + "; i++) {" + G.NL;

                                //node.Code += "for (int j = 0; j < " + n0 + "; j++) {" + G.NL;
                                
                                //node.Code += "string i = " + G.NL;

                                for (int i = 0; i < n0; i++)
                                {
                                    string loopVariable = null; //The line below emits "O.SetValFromCache(..., tempName)", same as a "STRING x = ..." statement
                                    string setLoopStringCs = CacheRefScalarCs(out loopVariable, node[0][i].nameSimpleIdent, w.scalarCache, w.headerCs, EScalarRefType.String, "x" + Num(node) + "_" + i + "[i]", true, true, false);
                                    node.Code += setLoopStringCs + G.NL;
                                }
                                node.Code += statements;
                                node.Code += "}" + G.NL;
                                //node.Code += "}" + G.NL;
                                node.Code += "} //end of try" + G.NL;  //assign var is always removed, also in case of error
                                node.Code += "finally {" + G.NL;
                                //node.Code += loopVariable + " = null;" + G.NL; //invalidates pointer
                                for (int i = 0; i < n0; i++)
                                {
                                    node.Code += "O.RemoveScalar(`" + node[0][i].nameSimpleIdent + "`);" + G.NL;
                                }
                                node.Code += "}  //end of finally" + G.NL;  //end of finally
                            }
                        }
                        break;

                    case "ASTFORSTATEMENTS":
                        {
                            GetCodeFromAllChildren(node);
                        }
                        break;
                    case "ASTIFSTATEMENTS":
                        {
                            GetCodeFromAllChildren(node);
                        }
                        break;
                    case "ASTELSESTATEMENTS":
                        {
                            GetCodeFromAllChildren(node);
                        }
                        break;
                    case "ASTFORVAL":
                        {
                            if (node[0].Text != "ASTFORLEFTSIDE" || node[1].Text != "ASTFORRIGHTSIDE" || node[2].Text != "ASTFORSTATEMENTS")
                            {
                                throw new GekkoException();
                            }
                            string nameSimpleIdent = node[0][0].nameSimpleIdent;
                            if (nameSimpleIdent == null)
                            {
                                G.Writeln2("*** ERROR: Composed names not (yet) allowed for loop variables");
                                throw new GekkoException();
                            }
                            string codeFrom = node[1][0].Code;
                            string codeEnd = node[1][1].Code;
                            string codeStep = null;
                            ASTNode stepNode = node[1][2];
                            if (stepNode == null) codeStep = "new ScalarVal(1d)";
                            else codeStep = node[1][2].Code;
                            string statements = node[2].Code;  //has been done in "ASTFORSTATEMENTS"
                            string tempName = "temp" + ++Globals.counter;                            
                            string startName = "start" + ++Globals.counter;
                            string endName = "end" + ++Globals.counter;
                            string stepName = "step" + ++Globals.counter;
                            //NOTE this will mean that the end and step are fixed when seeing the FOR. Should be ok. Alternative is crazy.                            

                            string loopVariable = null;
                            string setLoopStringCs = CacheRefScalarCs(out loopVariable, nameSimpleIdent, w.scalarCache, w.headerCs, EScalarRefType.Val, "double.NaN", false, true, false);
                            node.Code += setLoopStringCs + G.NL;                            
                            node.Code += "double " + stepName + " = " + codeStep + ".GetVal(t);" + G.NL;
                            node.Code += "double " + startName + " = " + codeFrom + ".GetVal(t);" + G.NL;
                            node.Code += "double " + endName + " = " + codeEnd + ".GetVal(t) + " + stepName + "/1000000d;" + G.NL;  //added a tiny bit of steplength, to guard against rounding errors
                            node.Code += "ScalarVal " + tempName + " = (ScalarVal)" + loopVariable + ";" + G.NL;
                            node.Code += "try {";                            
                            node.Code += "for (" + tempName + ".val = " + startName + " ; O.ContinueIterating(" + tempName + ".val, " + endName + ", " + stepName + "); " + tempName + ".val += " + stepName + ")";
                            node.Code += "{" + G.NL;
                            node.Code += statements;
                            node.Code += "}" + G.NL;
                            node.Code += "} //end of try" + G.NL;  //assign var is always removed, also in case of error
                            node.Code += "finally {" + G.NL;                            
                            node.Code += "O.RemoveScalar(`" + nameSimpleIdent + "`);" + G.NL;
                            node.Code += "}  //end of finally" + G.NL;  //end of finally                            
                        }
                        break;
                    case "ASTURL":
                        {
                            //node.Code = node[0].Code;
                            string s = null;
                            foreach (ASTNode child in node.ChildrenIterator())
                            {
                                if (child.IsFirstChild()) s = child.Code;
                                else s += ".Add(new ScalarString(`/`), t).Add(" + child.Code + ", t)";
                            }
                            node.Code += s;
                        }
                        break;
                    case "ASTURLPART":
                        {
                            string s = null;
                            foreach (ASTNode child in node.ChildrenIterator())
                            {
                                if (child.IsFirstChild()) s = child.Code;
                                else s += ".Add(new ScalarString(`.`), t).Add(" + child.Code + ", t)";
                            }
                            node.Code += s;
                        }
                        break;
                    case "ASTURLFIRST1":
                        {
                            node.Code = node[0].Code + ".Add(new ScalarString(`http://`, t)).Add(" + node[1].Code + ", t)";
                        }
                        break;
                    case "ASTURLFIRST3":                    
                        {
                            node.Code = node[0].Code;
                        }
                        break;
                    case "ASTFILENAME":
                        {
                            //node.Code = node[0].Code;
                            string s = null;                            
                            foreach (ASTNode child in node.ChildrenIterator())
                            {
                                if (child.IsFirstChild()) s = child.Code;
                                else s += ".Add(new ScalarString(`\\\\`), t).Add(" + child.Code + ", t)";
                            }
                            node.Code += s;
                        }
                        break;
                    case "ASTFILENAMEPART":
                        {
                            string s = null;
                            foreach (ASTNode child in node.ChildrenIterator())
                            {
                                if (child.IsFirstChild()) s = child.Code;
                                else s += ".Add(new ScalarString(`.`), t).Add(" + child.Code + ", t)";
                            }
                            node.Code += s;
                        }
                        break;
                    case "ASTFILENAMEFIRST1":                    
                        {
                            node.Code = node[0].Code + ".Add(new ScalarString(`:\\\\`), t).Add(" + node[1].Code + ", t)";
                        }
                        break;                    
                    case "ASTFILENAMEFIRST2":
                    case "ASTFILENAMEFIRST3":
                        {
                            node.Code = node[0].Code;
                        }
                        break;
                    case "ASTFUNCTION":
                        {
                            string functionName = node[0].Text.ToLower();  //no string composition allowed for functions.

                            if (functionName == "string") functionName = "tostring";
                            
                            //TODO: Should these just override??? And what if inbuilt function does not exist??

                            if (functionName == "dlog" || functionName == "dif" || functionName == "diff" || functionName == "pch")
                            {
                                if (node.ChildrenCount() > 2)
                                {
                                    G.Writeln2("*** ERROR: Expected 1 argument for " + functionName + "() function");
                                    throw new GekkoException();
                                }
                                string code = node[1].Code;

                                //#893243875
                                //HACK MEGA HACK MEGA HACK MEGA HACK MEGA HACK MEGA HACK MEGA HACK MEGA HACK MEGA
                                //HACK MEGA HACK MEGA HACK MEGA HACK MEGA HACK MEGA HACK MEGA HACK MEGA HACK MEGA
                                //HACK MEGA HACK MEGA HACK MEGA HACK MEGA HACK MEGA HACK MEGA HACK MEGA HACK MEGA
                                //HACK MEGA HACK MEGA HACK MEGA HACK MEGA HACK MEGA HACK MEGA HACK MEGA HACK MEGA
                                //HACK MEGA HACK MEGA HACK MEGA HACK MEGA HACK MEGA HACK MEGA HACK MEGA HACK MEGA 
                                //How to know if a MetaTimeSeries or ScalarVal gets returned from code here...??
                                if (code.StartsWith("O.Add(") || code.StartsWith("O.Divide(")  || code.StartsWith("O.Multiply(") || code.StartsWith("O.Negate(") || code.StartsWith("O.Power(") || code.StartsWith("O.Subtract("))
                                {
                                    //Do this some more robust way....
                                    //first one: ___(t___ --> ___(t.Add(-1)___
                                    //next one: ___, t___ --> ___, t.Add(-1)___
                                    //string codeLag = code.Replace("(" + Globals.functionT1Cs, "(" + Globals.functionT1Cs + ".Add(-1)");
                                    //codeLag = codeLag.Replace(", " + Globals.functionT1Cs, ", " + Globals.functionT1Cs + ".Add(-1)");

                                    //first one: ___(t)___ --> ___(t.Add(-1))___
                                    //first one: ___(t,___ --> ___(t.Add(-1),___
                                    //next one: ___, t,___ --> ___, t.Add(-1),___
                                    //next one: ___, t)___ --> ___, t.Add(-1))___
                                    string codeLag = code.Replace("(" + Globals.functionT1Cs + ")", "(" + Globals.functionT1Cs + ".Add(-1)" + ")");
                                    codeLag = codeLag.Replace("(" + Globals.functionT1Cs + ",", "(" + Globals.functionT1Cs + ".Add(-1)" + ",");
                                    codeLag = codeLag.Replace(", " + Globals.functionT1Cs + ",", ", " + Globals.functionT1Cs + ".Add(-1)" + ",");
                                    codeLag = codeLag.Replace(", " + Globals.functionT1Cs + ")", ", " + Globals.functionT1Cs + ".Add(-1)" + ")");
                                    
                                    if (functionName == "dlog")
                                    {
                                        node.Code += "Functions.log(" + Globals.functionT1Cs + ", " + code + ").Subtract(Functions.log(" + Globals.functionT1Cs + ", " + codeLag + "), " + Globals.functionT1Cs + ")";
                                    }
                                    else if (functionName == "dif" || functionName == "diff")
                                    {
                                        node.Code += "(" + code + ").Subtract(" + codeLag + " ," + Globals.functionT1Cs + ")";
                                    }
                                    else if (functionName == "pch")
                                    {
                                        node.Code += "(" + code + ").Divide(" + codeLag + ", " + Globals.functionT1Cs + ").Subtract(new ScalarVal(1d), " + Globals.functionT1Cs + ").Multiply(new ScalarVal(100d), " + Globals.functionT1Cs + ")";
                                    }
                                    else throw new GekkoException();
                                }
                                else
                                {
                                    if (functionName == "dlog")
                                    {
                                        node.Code += "Functions.dlog(" + Globals.functionT1Cs + ", " + code + ")";
                                    }
                                    else if (functionName == "dif" || functionName == "diff")
                                    {
                                        node.Code += "Functions.dif(" + Globals.functionT1Cs + ", " + code + ")";
                                    }
                                    else if (functionName == "pch")
                                    {
                                        node.Code += "Functions.pch(" + Globals.functionT1Cs + ", " + code + ")";
                                    }
                                }
                            }
                            else
                            {
                                if (w.functionUserDefined.ContainsKey(functionName))  //case-insensitive anyway
                                {
                                    node.Code += functionName + "(" + Globals.functionP1Cs + ", " + Globals.functionT1Cs + ", ";
                                }
                                else
                                {
                                    node.Code += "Functions." + functionName + "(" + Globals.functionT1Cs + ", ";
                                }
                                for (int i = 1; i < node.ChildrenCount(); i++)
                                {
                                    node.Code += node[i].Code;
                                    if (i < node.ChildrenCount() - 1) node.Code += ", ";
                                }                                                                

                                if (node.Code.EndsWith(", "))
                                {
                                    node.Code = node.Code.Substring(0, node.Code.Length - 2);
                                }
                                node.Code += ")";
                            }
                        }
                        break;
                    case "ASTFUNCTIONDEF":
                        {                            
                            //TODO: allow overloads
                            if (w.functionUserDefined.ContainsKey(w.functionHelper.functionName))
                            {
                                G.Writeln2("*** ERROR: User function with name '" + w.functionHelper.functionName + "' has already been defined");
                                throw new GekkoException();
                            }
                            w.functionUserDefined.Add(w.functionHelper.functionName, true);
                            //We use ToLower(), since user functions are not distinguished by means of capitalization
                            string lhsClassNameCode = G.GetVariableType(w.functionHelper.lhsTypes.Count);

                            if (w.functionHelper.lhsTypes.Count > 1)
                            {
                                //Create the classe corresponding to the return tuple (lhs)
                                string tupleClassName = G.GetVariableType(w.functionHelper.lhsTypes.Count);
                                CreateTupleClass(w.headerCs, w.functionHelper.lhsTypes.Count, tupleClassName, w.tupleClasses);
                            }

                            string method = "public static " + lhsClassNameCode + " " + w.functionHelper.functionName.ToLower() + "(" + Globals.functionP2Cs + ", " + Globals.functionT2Cs + ", ";

                            for (int i = 0; i < w.functionHelper.storage.Count; i++)
                            {
                                FunctionArgumentsHelperElements fah = w.functionHelper.storage[i];
                                //method += G.GetVariableType(fah.type) + " " + fah.parameterCode;                                
                                //TODO type checks...
                                if (fah.tupleCount > 1)
                                {                                    
                                    //Create the classe corresponding to the input tuple (in rhs params)
                                    string tupleClassName = G.GetVariableType(fah.tupleCount);
                                    CreateTupleClass(w.headerCs, fah.tupleCount, tupleClassName, w.tupleClasses);                                    
                                    //this is a tuple
                                    method += tupleClassName + " " + fah.tupleNameCode;
                                    i = i + (fah.tupleCount - 1);  //we skip the rest of these tuples here!
                                }
                                else
                                {
                                    method += "IVariable" + " " + fah.parameterCode;
                                }
                                method += ", ";                                
                            }
                            if (method.EndsWith(", ")) method = method.Substring(0, method.Length - 2);  //we remove the last ", "
                            method += ") {" + G.NL;

                            method += node[3].Code + G.NL;  //should always be subnode #4
                            method += "}" + G.NL;
                            w.headerCs.AppendLine(method);
                            w.functionHelper = null;  //do not remove this line: important!
                        }
                        break;
                    case "ASTFUNCTIONDEFARGS":
                        {
                            //No code to be harvested, function args are completely primitive {type, name} items put in functionArguments container.
                        }
                        break;
                    case "ASTFUNCTIONDEFARG":
                        {
                            //No code to be harvested, function args are completely primitive {type, name} items put in functionArguments container.
                            //handled in ASTFUNCTIONDEFRHSSIMPLE and ASTFUNCTIONDEFRHSTUPLE
                        }
                        break;
                    case "ASTFUNCTIONDEFRHSTUPLE":
                        {                            
                            //TODO TODO TODO check that args have different names     
                            int counter = 0;
                            int paramCount = -12345;
                            foreach (ASTNode child in node.ChildrenIterator())
                            {
                                counter++;
                                if (counter == 1) paramCount = w.functionHelper.storage.Count;
                                FunctionArgumentsHelperElements fah = new FunctionArgumentsHelperElements();                                
                                fah.parameterName = child[1].Text;
                                fah.type = child[0].Text;
                                fah.tupleNameCode = "param_" + fah.type + "_" + paramCount;
                                fah.parameterCode = "param_" + fah.type + "_" + paramCount + ".tuple" + (counter - 1);
                                fah.tupleCount = node.ChildrenCount();
                                w.functionHelper.storage.Add(fah);
                            }                            
                        }
                        break;
                    case "ASTFUNCTIONDEFRHSSIMPLE":
                        {
                            //TODO TODO TODO check that args have different names                         
                            ASTNode child = node[0];
                            FunctionArgumentsHelperElements fah = new FunctionArgumentsHelperElements();
                            fah.parameterName = child[1].Text;
                            fah.type = child[0].Text;
                            fah.parameterCode = "param_" + fah.type + "_" + fah.parameterName + w.functionHelper.storage.Count;
                            w.functionHelper.storage.Add(fah);
                        }
                        break;                    
                    case "ASTFUNCTIONDEFCODE":
                        {
                            GetCodeFromAllChildren(node);
                        }
                        break;
                    case "ASTFUNCTIONDEFLHSTUPLE":
                        {
                            //do nothing
                        }
                        break;                    
                    case "ASTFUNCTIONDEFTYPE":
                            {
                                if (node[0].Text == "ASTFUNCTIONDEFLHSTUPLE")
                                {                                    
                                    foreach (ASTNode child in node[0].ChildrenIterator())
                                    {
                                        w.functionHelper.lhsTypes.Add(child.Text);
                                    }                                    
                                }
                                else
                                {
                                    w.functionHelper.lhsTypes.Add(node[0].Text);
                                }

                            }
                            break;
                    case "ASTFUNCTIONDEFNAME":
                        {
                            w.functionHelper.functionName = node[0].Text;
                        }
                        break;
                    case "ASTGENERIC1":
                        {
                            //This catches name:name or identDigit.                            
                            if (node.ChildrenCount() == 1)
                            {                                
                                //identDigit
                                node.Code = node[0].Code;
                            }
                            else
                            {                                
                                //name:name
                                node.Code = "O.Add(" + node[0].Code + ", new ScalarString(`:`), " + node[1].Code + ", t)";                             
                            }
                        }
                        break;
                    case "ASTSERIESQUESTION":
                        {
                            //GENR fy = ...                            
                            node.Code += "O.SeriesQuestion();" + G.NL;
                        }
                        break;
                    case "ASTGENR":                    
                        {
                            //GENR fy = ...                            
                            node.Code += HandleGenr(Num(node), node[0].Code, node[1].Code, node[2].Code, w, null);
                        }
                        break;
                    case "ASTGENRLHSFUNCTION":
                        {
                            //GENR dlog(fy) = ...                            
                            node.Code += HandleGenr(Num(node), node[0].Code, node[1].Code, node[2].Code, w, node[3].Text);
                        }
                        break;
                    case "ASTGENRINDEXER":
                        {
                            //GENR fy[2015] = ...
                            node.Code += "O.GetTimeSeries(" + node[0].Code + ").SetData(O.GetDate(" + node[1].Code + "), O.GetVal(" + node[2].Code + ", t));" + G.NL;
                        }
                        break;
                    case "ASTMATRIXINDEXER":
                        {
                            //MATRIX a[3, 5] = ...
                            ASTNode nodeLeftSide = node[0];
                            if (nodeLeftSide.nameSimpleIdent != null)
                            {
                                AstListHelper(nodeLeftSide, w, nodeLeftSide.nameSimpleIdent, false);
                            }
                            else throw new GekkoException();

                            node.Code += "O.GetMatrix(" + nodeLeftSide.Code + ").SetData(" + node[1].Code + ", " + node[2].Code + ", " + node[3].Code + ");" + G.NL;

                            //node.Code += "O.GetMatrix(new ScalarString(`" + nodeLeftSide.nameSimpleIdent + "`)).SetData(" + node[1].Code + ", " + node[2].Code + ", " + node[3].Code + ");" + G.NL;

                            //node.Code += "O.SetMatrixData(new ScalarString(`" + nodeLeftSide.nameSimpleIdent + "`), " + node[1].Code + ");" + G.NL;

                        }
                        break;
                    case "ASTMATRIX":
                        {
                            //MATRIX a = matrix(100, 100);  etc.
                            //node.Code += "O.SetMatrix(" + node[0].Code + ", " + node[1].Code + ");" + G.NL;

                            if (node[0].Text == "?")
                            {
                                
                                if (node.ChildrenCount() > 1)
                                {
                                    node.Code += "O.Matrix2.Q(" + Globals.QT + node[1].Text + Globals.QT + ");" + G.NL;
                                }
                                else
                                {
                                    node.Code += "O.Matrix2.Q();" + G.NL;
                                }
                            }
                            else
                            {

                                ASTNode nodeLeftSide = node[0];
                                if (nodeLeftSide.nameSimpleIdent != null)
                                {
                                    AstListHelper(nodeLeftSide, w, nodeLeftSide.nameSimpleIdent, false);
                                }
                                else throw new GekkoException();
                                //node.Code += "O.SetMatrixData(" + node[0].Code + ", `" + nodeLeftSide.nameSimpleIdent + "`, " + node[1].Code + ");" + G.NL;
                                node.Code += "O.SetMatrixData(new ScalarString(`" + nodeLeftSide.nameSimpleIdent + "`), " + node[1].Code + ");" + G.NL;
                            }
                        }
                        break;
                    case "ASTGENRLISTINDEXER":
                        {
                            node.Code += HandleGenr(Num(node), node[0].Code, "O.GetTimeSeriesFromList(" + node[1].Code + ", " + node[2].Code + ", 1, t)", node[3].Code, w, null);
                            //GENR #m[2] = ...
                        }
                        break;
                    case "ASTGENRLISTINDEXER2":
                        {
                            //GENR #m[2][2015] = ...
                            node.Code += "O.GetTimeSeriesFromList(" + node[0].Code + ", " + node[1].Code + ", 1, t).ts.SetData(O.GetDate(" + node[2].Code + "), O.GetVal(" + node[3].Code + ", t));" + G.NL;
                        }
                        break;
                    case "ASTHASHPAREN":
                        {
                            node.Code = node[0].Code;
                        }
                        break;
                    case "ASTMATRIXROW":
                        {
                            node.Code = "O.MatrixRow(";
                            for (int i = 0; i < node.ChildrenCount(); i++)
                            {
                                node.Code += node[i].Code;
                                if (i < node.ChildrenCount() - 1) node.Code += ", ";
                            }
                            node.Code += ")";
                        }
                        break;
                    case "ASTMATRIXCOL":
                        {
                            node.Code = "O.MatrixCol(";
                            for (int i = 0; i < node.ChildrenCount(); i++)
                            {
                                node.Code += node[i].Code;
                                if (i < node.ChildrenCount() - 1) node.Code += ", ";
                            }
                            node.Code += ")";

                        }
                        break;
                    case "ASTHASH":
                        {
                            string simpleIdent = null;
                            bool stringify = false;
                            if (node.ChildrenCount() > 0 && node[0].Text == "ASTDOLLARHASHNAMESIMPLE") stringify = true;
                            if (node.ChildrenCount() > 0 && (node[0].Text == "ASTHASHNAMESIMPLE" || node[0].Text == "ASTDOLLARHASHNAMESIMPLE"))
                            {
                                simpleIdent = node[0][0].Text;
                            }

                            if (node[0].Text == "ASTLISTFILE")
                            {
                                node.Code += "O.ZListFile(O.GetString(" + node[0][0].Code + "))";
                            }
                            else
                            {

                                if (simpleIdent != null)
                                {
                                    string fa = FindFunctionArguments(node, w, simpleIdent);
                                    if (fa != null)
                                    {
                                        node.Code += fa;
                                    }
                                    else
                                    {
                                        AstListHelper(node, w, simpleIdent, stringify);
                                    }
                                }
                                else
                                {
                                    node.Code += "O.ZList(" + node[0].Code + ")";
                                }
                            }
                            break;
                        }
                    //case "ASTHASHNAMESIMPLE":
                    //    {
                    //        node.Code = "O.GetListFromCache(`" + node[0].Text + "`)";
                    //    }
                    //    break;
                    case "ASTIDENT":
                    case "ASTIDENTDIGIT":
                        {
                            node.Code = "new ScalarString(`" + node[0].Text + "`)";
                        }
                        break;

                    case "ASTIMPORT":
                        {
                            node.Code += Globals.clearTsCsCode + G.NL;
                            node.Code += "O.Import o" + Num(node) + " = new O.Import();" + G.NL;
                            node.Code += "o" + Num(node) + ".p = p;";
                            GetCodeFromAllChildren(node);
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTDOWNLOAD":
                        {
                            node.Code += "O.Link o" + Num(node) + " = new O.Link();" + G.NL;
                            string s = null;
                            if (node[0].Text != null) s = "http://";
                            node.Code += "o" + Num(node) + ".dbUrl = `" + s + "` + O.GetString(" + node[1].Code + ");" + G.NL;
                            node.Code += node[2].Code;  //fileName
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTEMPTYRANGEELEMENT":
                        {
                            node.Code = "null";
                        }
                        break;                        
                    case "ASTINDEX":  //the INDEX command
                        {
                            //TODO: handle listfile                        
                            node.Code += "O.Index o" + Num(node) + " = new O.Index();" + G.NL;

                            if (node.ChildrenCount() > 1)
                            {
                                node.Code += "o" + Num(node) + ".name = O.GetString(" + node[1].Code + ");" + G.NL;
                            }
                            else
                            {
                                node.Code += "o" + Num(node) + ".name = null;";  //just show the result
                            }
                            //node.Code += "o" + O(node) + ".listItems = O.GetList(O.IndexerAlone(" + node[0].Code + "));" + G.NL;
                            node.Code += "o" + Num(node) + ".listItems = O.GetList(" + node[0].Code + ");" + G.NL;
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTINDEXERALONE":  //indexer with nothing at the left: [a*], not #z[a*]. For ASTINDEXER, see "["
                        {
                            //Only 1 dimension supported                        
                            if (node.ChildrenCount() > 1) throw new GekkoException();
                            node.Code += "O.Indexer(null, " + node[0].Code + ", t)";  //null signals that it has nothing on the left
                        }
                        break;
                    case "ASTINDEXERELEMENT":  //For ASTINDEXER, see "["
                    case "ASTINDEXERELEMENTPLUS":
                        {
                            if (node.ChildrenCount() == 2)
                            {
                                if (node[0].Code != null)
                                {
                                    node.Code += "(" + node[0].Code + ")" + ".Add(new ScalarString(\":\"), t).Add(" + node[1].Code + ", t)";
                                }
                                else
                                {
                                    node.Code += node[1].Code;
                                }
                            }
                            else if (node.ChildrenCount() == 3)
                            {
                                if (node.Text == "ASTINDEXERELEMENTPLUS")
                                {
                                    G.Writeln2("*** ERROR: You cannot use '+' as starting character in ranges");
                                    throw new GekkoException();
                                }
                                if (node[0].Code != null)
                                {
                                    node.Code += "new IVariablesFilterRange((" + node[0].Code + ")" + ".Add(new ScalarString(\":\"), t).Add(" + node[1].Code + ", t), " + node[2].Code + ")";
                                }
                                else
                                {
                                    node.Code += "new IVariablesFilterRange(" + node[1].Code + ", " + node[2].Code + ")";
                                }
                            }
                            else throw new GekkoException();
                        }
                        break;
                    case "ASTINDEXERELEMENTBANK":                    
                        {
                            GetCodeFromAllChildren(node);                            
                        }
                        break;
                    case "ASTINTEGER":
                        {
                            //TODO 
                            //TODO 
                            //TODO use cache to avoid dublets
                            //TODO 
                            //TODO 
                            string minus = HandleNegate(node);
                            string intWithNumber = "i" + ++Globals.counter;
                            string s = "new ScalarVal(" + minus + node[0].Text + "d)";
                            w.headerCs.AppendLine("public static readonly ScalarVal " + intWithNumber + " = " + s + ";");
                            node.Code = intWithNumber;  //no need for checking if it exists
                        }
                        break;
                    case "ASTINTEGERNEGATIVE":
                        {
                            node.Code += node[0].Code;
                        }
                        break;
                    case "ASTMISSING":
                        {
                            //TODO 
                            //TODO 
                            //TODO use cache to avoid dublets
                            //TODO 
                            //TODO                             
                            string intWithNumber = "d" + ++Globals.counter;
                            string s = "new ScalarVal(double.NaN)";
                            w.headerCs.AppendLine("public static readonly ScalarVal " + intWithNumber + " = " + s + ";");
                            node.Code = intWithNumber;  //no need for checking if it exists
                        }
                        break;
                    case "ASTLIST":
                        {
                            if (node[0].Text == "?")
                            {
                                if (node.ChildrenCount() < 2)
                                {
                                    node.Code += "Program.List(`?`, null, null, false);" + G.NL;
                                }
                                else
                                {
                                    node.Code += "O.List.Q(" + Globals.QT + node[1].Text + Globals.QT + ");" + G.NL;
                                }
                            }
                            else
                            {                                
                                node.Code += HandleList(node, node[1].Code);
                            }
                        }
                        break;
                    case "ASTLISTFILE":
                        {
                            node.Code += node[0].Code;
                        }
                        break;
                    case "ASTRANGEWITHBANK":
                    case "ASTWILDCARDWITHBANK":
                    case "ASTLISTITEMWILDRANGE": //can be deleted, does not exist anymore
                        {
                            string bankCs = node[0].Code;
                            if (node.ChildrenCount() == 2)
                            {
                                //wildcard   
                                if (bankCs == null)
                                {
                                    node.Code = node[1].Code;
                                }
                                else
                                {
                                    node.Code = "(" + bankCs + ").Add(new ScalarString(\":\"), t).Add(" + node[1].Code + ", t)";
                                }
                             
                            }
                            else
                            {
                                if (bankCs == null)
                                {
                                    node.Code = "(" + node[1].Code + ").Add(new ScalarString(\"..\"), t).Add(" + node[2].Code + ", t)";
                                }
                                else
                                {
                                    //range
                                    node.Code = "(" + bankCs + ").Add(new ScalarString(\":\"), t).Add(" + node[1].Code + ", t).Add(new ScalarString(\"..\"), t).Add(" + node[2].Code + ", t)";
                                }
                            }                            
                        }
                        break;
                    case "ASTLISTITEMWILDRANGEBANK":
                        {
                            if (node.ChildrenCount() > 0)
                            {                            
                                node.Code += node[0].Code;
                            }
                        }
                        break;
                    case "ASTLISTITEM":
                        {
                            List<string> ss = new List<string>();
                            int variableIndex = 0;  //0 or 1
                            string number = "";
                            if (node.Parent.Text == "ASTLISTITEMS0") number = "0";
                            else if (node.Parent.Text == "ASTLISTITEMS1") number = "1";
                            else if (node.Parent.Text == "ASTLISTITEMS2") number = "2";
                            if (node.ChildrenCount() > 1) variableIndex = 1;
                            
                            ASTNode child = node[variableIndex];  //child is the variable, not the bank

                            if (child.Text == "ASTNAMEWITHBANK")
                            {
                                //a
                                //b:a
                                node.Code = "o" + Num(child) + ".listItems" + number + ".AddRange(O.GetList(" + AstBankHelper(child, w, 1) + "));" + G.NL;
                            }
                            else if (child.Text == "ASTLISTITEMWILDRANGE")
                            {
                                //f*nz
                                node.Code = "o" + Num(child) + ".listItems" + number + ".Add(O.GetString(" + child.Code + "));" + G.NL;
                            }
                            else if ((child.Text == "NEGATE" && child.ChildrenCount() > 0 && child[0].Text == "ASTNAMEWITHBANK"))
                            {
                                //-a
                                node.Code = "o" + Num(child) + ".listItems" + number + ".AddRange(O.GetList(" + AstBankHelper(child, w, 2) + "));" + G.NL;
                            }
                            else if (child.Text == "ASTLISTWITHBANK")
                            {
                                //bank2:#m, interpreted as bank2:m1, bank2:m2, bank2:m3, ...
                                node.Code = "o" + Num(child) + ".listItems" + number + ".AddRange(O.GetList(" + AstBankHelperList(child, w) + "));" + G.NL;
                            }
                            else
                            {
                                //expression, for instance
                                //'a'
                                //'b:a'
                                //#m[2]
                                node.Code = "o" + Num(child) + ".listItems" + number + ".AddRange(O.GetList(" + child.Code + "));" + G.NL;
                            }


                            if (variableIndex == 0)
                            {
                                //normal
                                //node.Code += node[0].Code;
                            }
                            else if (variableIndex == 1)
                            {
                                //node.Code += node[1].Code;
                                string cs = null;
                                if (node[0].Text == "ASTNAMEWITHBANK")
                                {
                                    cs = AstBankHelper(node[0], w, 1);
                                }
                                else
                                {
                                    cs = node[0].Code;
                                }
                                node.Code += "o" + Num(node) + ".listItems" + number + " = O.AddBankToListItems(o" + Num(node) + ".listItems" + number + ", O.GetString(" + cs + "));" + G.NL;
                            }
                            else throw new GekkoException();

                        }
                        break;
                    case "ASTLISTITEMS":
                        {
                            node.Code += "o" + Num(node) + ".listItems = new List<string>();" + G.NL;
                            GetCodeFromAllChildren(node);                            
                        }
                        break;                    
                    case "ASTLISTITEMS0":
                        {
                            node.Code += "o" + Num(node) + ".listItems0 = new List<string>();" + G.NL;
                            GetCodeFromAllChildren(node);
                        }
                        break;
                    case "ASTLISTITEMS1":
                        {
                            node.Code += "o" + Num(node) + ".listItems1 = new List<string>();" + G.NL;
                            GetCodeFromAllChildren(node);
                        }
                        break;
                    case "ASTLISTITEMS2":
                        {
                            node.Code += "o" + Num(node) + ".listItems2 = new List<string>();" + G.NL;
                            GetCodeFromAllChildren(node);
                        }
                        break;          
                    case "ASTLISTPREFIX":
                        {
                            if (node.ChildrenCount() == 1 && node[0].Text == "ASTNAMEWITHBANK")
                            {
                                node.Code = "o" + Num(node[0]) + ".listPrefix = O.GetString(" + AstBankHelper(node[0], w, 1) + ");" + G.NL;
                            }
                            else
                            {
                                node.Code += "o" + Num(node) + ".listPrefix = O.GetString(" + node[0].Code + ");" + G.NL;
                            }
                        }
                        break;
                    case "ASTLISTSUFFIX":
                        {
                            if (node.ChildrenCount() == 1 && node[0].Text == "ASTNAMEWITHBANK")
                            {
                                node.Code = "o" + Num(node[0]) + ".listSuffix = O.GetString(" + AstBankHelper(node[0], w, 1) + ");" + G.NL;
                            }
                            else
                            {
                                node.Code += "o" + Num(node) + ".listSuffix = O.GetString(" + node[0].Code + ");" + G.NL;
                            }
                        }
                        break;
                    case "ASTLISTSTRIP":
                        {
                            if (node.ChildrenCount() == 1 && node[0].Text == "ASTNAMEWITHBANK")
                            {
                                node.Code = "o" + Num(node[0]) + ".listStrip = O.GetString(" + AstBankHelper(node[0], w, 1) + ");" + G.NL;
                            }
                            else
                            {
                                node.Code += "o" + Num(node) + ".listStrip = O.GetString(" + node[0].Code + ");" + G.NL;
                            }
                        }
                        break;
                    case "ASTLISTSORT":
                        {
                            node.Code += "o" + Num(node) + ".listSort = O.GetString(" + node[0].Code + ");" + G.NL;
                        }
                        break;
                    case "ASTM":
                        {
                            node.Code += AddPrintCode("m", node[0].Code, node.Parent.Parent.Text, node);
                        }
                        break;
                    case "ASTV":
                        {
                            node.Code += AddPrintCode("v", node[0].Code, node.Parent.Parent.Text, node);
                        }
                        break;
                    case "ASTMEM":
                        {
                            node.Code += "Program.Mem(null);" + G.NL;
                        }
                        break;
                    case "ASTGOTO":
                        {
                            node.Code += "goto " + node[0].Text.ToLower().Trim() + ";" + G.NL;  //calls a C# label
                            w.wh.isGotoOrTarget = true;
                        }
                        break;
                    case "ASTLABEL":  //AREMOS: target
                        {
                            node.Code += node[0].Text.ToLower().Trim() + ":;" + G.NL;  //a C# label
                            w.wh.isGotoOrTarget = true;
                        }
                        break;
                    case "ASTMODEL":
                        {
                            node.Code += "O.Model o" + Num(node) + " = new O.Model();" + G.NL;
                            node.Code += "o" + Num(node) + ".p = p;";
                            GetCodeFromAllChildren(node); //gets filename
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;                    
                    case "ASTMP":
                        {
                            node.Code += AddPrintCode("mp", node[0].Code, node.Parent.Parent.Text, node);
                        }
                        break;
                    case "ASTMULBK":
                        {
                            node.Code += Globals.clearTsCsCode + G.NL;
                            node.Code += "O.Mulbk o" + Num(node) + " = new O.Mulbk();" + G.NL;
                            GetCodeFromAllChildren(node);
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTN":
                        {
                            node.Code += AddPrintCode("n", node[0].Code, node.Parent.Parent.Text, node);
                        }
                        break;
                    case "ASTNAME":
                        {
                            if (node.ChildrenCount() > 0)
                            {
                                int counter = 0;
                                foreach (ASTNode child in node.ChildrenIterator())
                                {
                                    if (counter == 0)
                                    {
                                        node.Code += "(" + child.Code + ")";                                        
                                    }
                                    else node.Code += ".Add(" + child.Code + ", t)";
                                    counter++;
                                }
                                if (node.ChildrenCount() == 1 && node[0].Text == "ASTIDENT") node.nameSimpleIdent = node[0][0].Text;
                            }
                            else
                            {
                                //do nothing, probably we can never end here
                            }
                        }
                        break;
                    case "ASTNAMEWITHDOT":
                        {
                            //Can only have 2 ASTNAME's as children
                            if (node.ChildrenCount() == 1)
                            {                                
                                //no dot in this name
                                node.Code += node[0].Code;
                                node.nameSimpleIdent = node[0].nameSimpleIdent;  //inherits this info
                            }
                            else if (node[1].Text == "ASTINTEGER")
                            {
                                //stuff like fy.1
                                if (Globals.useDotFunctionalityInParser)
                                {
                                    //for instance in fy.1                            
                                    node.Code += node[0].Code;
                                    node.nameSimpleIdent = node[0].nameSimpleIdent;  //inherits this info
                                    node.dotNumber = node[1].Code;
                                }
                                else
                                {
                                    G.Writeln2("*** ERROR: Problem with .1, .2, etc. -- please use [-1], [-2], etc.");
                                    throw new GekkoException();
                                    node.Code += node[0].Code;
                                    node.nameSimpleIdent = node[0].nameSimpleIdent;  //inherits this info
                                }

                            }
                            else
                            {
                                ////NOT ACTIVE AT THE MOMENT
                                ////NOT ACTIVE AT THE MOMENT (versions, like fY.sim1)
                                ////NOT ACTIVE AT THE MOMENT
                                //G.Writeln2("*** ERROR: name with versions (e.g. fY.s0) not allowed at this point");
                                //throw new GekkoException();
                                node.Code += "O.Add(O.Add(" + node[0].Code + ", new ScalarString(`.`), t), " + node[1].Code + ", t)";
                                if (node[0].nameSimpleIdent != null && node[1].nameSimpleIdent != null)
                                {
                                    node.nameSimpleIdent = node[0].nameSimpleIdent + "." + node[1].nameSimpleIdent;
                                    node.Code = "new ScalarString(`" + node.nameSimpleIdent + "`)";  //overrides
                                }
                                else
                                {
                                }
                            }

                        }
                        break;
                    case "ASTNAMESLIST":
                        {
                            node.Code += "o" + Num(node) + ".namesList = new List<string>();" + G.NL;
                            foreach (ASTNode child in node.ChildrenIterator())
                            {
                                node.Code += "o" + Num(node) + ".namesList.Add(O.GetString(" + child.Code + "));" + G.NL;
                            }
                        }
                        break;
                    case "ASTLISTWITHBANK":
                        {
                            node.Code += AstBankHelperList(node, w);
                        }
                        break;
                    case "ASTNAMEWITHBANK":
                        {
                            //Must always have 2 children, ASTBANK and ASTNAMEWITHDOT
                            string lagTypeCs = null;
                            if (node[1].Text == "ASTNAMEWITHDOT")  //probably is always so, but we check it.
                            {
                                if (Globals.useDotFunctionalityInParser)
                                {
                                    lagTypeCs = node[1].dotNumber;
                                }
                            }                            

                            if (node[0].ChildrenCount() == 0 && node[1].ChildrenCount() == 1 && node[1][0].Text == "ASTNAME" && node[1][0].ChildrenCount() == 1 && node[1][0][0].Text == "ASTSCALAR")
                            {
                                G.Writeln2("*** ERROR #24737643");
                                throw new GekkoException();
                                ////For instance this structure corresponding to "%b". This is interpreted as a VAL scalar even though it might be a STRING scalar pointing to a timeseries.
                                ////ASTNAMEWITHBANK
                                ////  ASTBANK
                                ////  ASTNAMEWITHDOT
                                ////    ASTNAME
                                ////      ASTSCALAR
                                ////        ASTPERCENTNAMESIMPLE
                                ////          b
                                //node[1][0][0].Code = null;  //sub-nodes have been visited: this result gets overridden
                                //HandleScalar(node[1][0][0], false, wh2);
                                //node.Code = node[1][0][0].Code;                                
                            }
                            else
                            {

                                string code = AstBankHelper(node, w, 0);
                                if (Globals.useDotFunctionalityInParser && lagTypeCs != null)
                                {
                                    //This is a fY.1 type of variable.
                                    //Why does this work, and why is 'code' not used??
                                    node.Code = "O.Indexer(" + node.Code + ", " + lagTypeCs + ", t)";
                                }
                                else
                                {
                                    node.Code += code;
                                }
                            }
                        }
                        break;
                    case "ASTNO":
                        {
                            node.Code = "new ScalarString(`no`)";
                        }
                        break;
                    case "NEGATE":
                        //HMMMMMMMMMMM, ASTNEGATE better name
                        {

                            if (node.IgnoreNegate)
                            {
                                //if marked as ignore-node, the after-stuff is not done for this node (typically a sub-node has signalled this).
                                node.Code = node.GetChildCode(0);
                            }
                            else
                            {
                                node.Code += "O.Negate(" + node.GetChildCode(0) + ", t)";
                            }

                        }
                        break;
                    case "ASTOPEN":
                        {
                            node.Code += Globals.clearTsCsCode + G.NL;
                            node.Code += "O.Open o" + Num(node) + " = new O.Open();" + G.NL;
                            GetCodeFromAllChildren(node);
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTOPENHELPER":
                        {
                            string as2 = null;
                            if (node[1].ChildrenCount() == 1) as2 = node[1][0].Text;                            
                            node.Code += "o" + Num(node) + ".openFileNames.Add(new List<string>() {O.GetString(" + node[0].Code + "), `" + as2 + "`});" + G.NL;
                        }
                        break;
                    case "ASTOPT_":
                        {
                            //See the stuff just before this big switch statement, ASTOPT_STRING_xxx etc.
                            //Gets the stuff copied to this node, so we do not have to do this manually
                            GetCodeFromAllChildren(node);
                        }
                        break;
                    case "ASTOPTION":
                        {
                            if (node.GetChild(0).Text == "?")
                            {
                                node.Code += "Program.PrintOptions(`Program.options`);";
                            }
                            else
                            {
                                string o = "";
                                //Use GekkoStringBuilder??
                                StringBuilder s = new StringBuilder();
                                CreateOptionVariable(node, s, ref o);
                                node.Code += s.ToString();
                                if (o == "freq")
                                {
                                    node.Code += Globals.clearTsCsCode + G.NL;
                                    node.Code += "Program.AdjustFreq();";
                                }
                                if (o == "interface_sound_type")
                                {
                                    if (!p.hasBeenCmdFile)
                                    {
                                        node.Code += "Program.PlaySound();";
                                    }
                                }
                                if (o == "folder_menu" || o == "menu_startfile")
                                {
                                    node.Code += "CrossThreadStuff.RestartMenuBrowser();";
                                }
                                if (o == "interface_zoom")
                                {
                                    node.Code += "CrossThreadStuff.Zoom();";
                                }


                                if (o == "folder_working")
                                {
                                    //s.AppendLine("Gui.ChangeWorkingFolder(Program.options.folder_working);");
                                    //Gui.ChangeWorkingFolder(Program.options.folder_working);
                                    node.Code += "CrossThreadStuff.WorkingFolder(``);";
                                }
                                if (o == "solve_gauss_reorder")
                                {
                                    node.Code += "G.Writeln();";
                                    node.Code += "G.Writeln(`+++ NOTE: Reorder: you must issue a MODEL statement afterwards, for this option to take effect.`);";
                                    node.Code += "G.Writeln(`+++       (In command files, place this option before any MODEL statements).`);";
                                }
                                if (o == "databank_file_format")
                                {
                                    node.Code += "Globals.hasBeenTsdTsdxOptionChangeSinceLastClear = true;";
                                }
                                if (o == "timefilter_type")  //TODO: only issue if really avg
                                {
                                    node.Code += "G.Writeln();";
                                    node.Code += "G.Writeln(`+++ NOTE: Timefilter type = 'avg' only works for PRT and MULPRT.`);";
                                }
                                //solve_gauss_dump
                            }
                        }
                        break;
                    case "ASTOPT1":  //PRT-type statement
                    case "ASTOPT2":  //PRT-type statement
                        {
                            //node.Code += "codes.Add(`" + node[0].Text + "`);" + G.NL;
                            GetCodeFromAllChildren(node);
                        }
                        break;
                    case "ASTP":
                        {
                            node.Code += AddPrintCode("p", node[0].Code, node.Parent.Parent.Text, node);
                        }
                        break;
                    case "ASTPCH":
                        {
                            node.Code += AddPrintCode("pch", node[0].Code, node.Parent.Parent.Text, node);
                        }
                        break;
                    case "ASTABS":
                        {
                            node.Code += AddPrintCode("abs", node[0].Code, node.Parent.Parent.Text, node);
                        }
                        break;
                    case "ASTDIF":
                        {
                            node.Code += AddPrintCode("dif", node[0].Code, node.Parent.Parent.Text, node);
                        }
                        break;
                    case "ASTLEV":
                        {
                            node.Code += AddPrintCode("lev", node[0].Code, node.Parent.Parent.Text, node);
                        }
                        break;
                    case "ASTPERCENTNAMESIMPLE":
                        {
                            node.Code = "`" + node[0].Text + "`";
                        }
                        break;
                    case "ASTDOLLARPERCENTNAMESIMPLE":
                        {
                            node.Code = "`" + node[0].Text + "`";
                        }
                        break;
                    case "ASTPERCENTPAREN":
                        {
                            node.Code = "O.GetString(" + node[0].Code + ")";
                        }
                        break;
                    case "ASTOLS":
                    case "ASTPRT":
                        {
                            if (node.Text == "ASTOLS")
                            {
                                node.Code += "O.Ols o" + Num(node) + " = new O.Ols();" + G.NL;
                            }
                            else
                            {
                                //PRT
                                node.Code += "O.Prt o" + Num(node) + " = new O.Prt();" + G.NL;
                            }
                            //node.Code += "o" + Num(node) + ".t1 = G.GetStartDate(o" + Num(node) + ".t1);" + G.NL;
                            //node.Code += "o" + Num(node) + ".t2 = G.GetEndDate(o" + Num(node) + ".t2);" + G.NL;
                            GetCodeFromAllChildren(node);                            

                            if (node.Text == "ASTPRT")
                            {
                                Globals.lastPrtCsSnippet = node.Code;  //without the o117.Exe()
                                Globals.lastPrtCsSnippet += "return o" + Num(node) + ";" + G.NL;
                                Globals.lastPrtCsSnippetHeader = w.headerCs.ToString();  //may contain a lot of unnecessary IVariables, but never mind (not a problem when used interactively)
                            }

                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;

                        }
                        break;
                    case "ASTPRTTYPE":
                        {
                            node.Code += "o" + Num(node) + ".prtType = `" + node[0].Text + "`;" + G.NL;
                        }
                        break;
                    case "ASTPRTOPTION":
                        {
                            node.Code += node[0].Code + G.NL;
                        }
                        break;
                    case "ASTPRTELEMENTOPTIONFIELD":
                        {                            
                            GetCodeFromAllChildren(node);
                        }
                        break;
                    case "===ASTPRTROWS":
                        {
                            node.Code = node[0].Code;
                        }
                        break;
                    case "ASTNEWTABLE":
                        {
                            node.Code += "Program.CreateNewTable(O.GetString(" + node[0].Code + "));" + G.NL;
                        }
                        break;
                    case "ASTTABLENEXT":
                        {
                            node.Code += "Program.GetTable(O.GetString(" + node[0].Code + ")).CurRow.Next();";
                        }
                        break;
                    case "ASTTABLEPRINT":
                        {
                            if (node.ChildrenCount() > 1)
                            {
                                node.Code += "Program.PrintTable(Program.GetTable(O.GetString(" + node[0].Code + ")), O.GetString(" + node[1].Code + ");" + G.NL;
                            }
                            else
                            {
                                node.Code += "Program.PrintTable(Program.GetTable(O.GetString(" + node[0].Code + ")), null);" + G.NL;
                            }
                        }
                        break;
                    case "ASTTABLESETVALUES":
                        {
                            node.Code += "O.Table.SetValues o" + Num(node) + " = new O.Table.SetValues();" + G.NL;
                            node.Code += "o" + Num(node) + ".name = O.GetString(" + node[0].Code + ");" + G.NL;
                            node.Code += "o" + Num(node) + ".col = O.GetInt(" + node[1].Code + ");" + G.NL;
                            node.Code += "o" + Num(node) + ".t1 = O.GetDate(" + node[2].Code + ");" + G.NL;
                            node.Code += "o" + Num(node) + ".t2 = O.GetDate(" + node[3].Code + ");" + G.NL;
                            node.Code += node[4].Code;
                            node.Code += "o" + Num(node) + ".printcode = O.GetString(" + node[5].Code + ");" + G.NL;
                            node.Code += "o" + Num(node) + ".scale = O.GetVal(" + node[6].Code + ", t);" + G.NL;
                            node.Code += "o" + Num(node) + ".format = O.GetString(" + node[7].Code + ");" + G.NL;
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;                            
                        }
                        break;

                    case "ASTTABLESETTOPBORDER":
                    case "ASTTABLESETBOTTOMBORDER":
                        {
                            string name = node.GetChild(0).Text;
                            //CheckCurrow(node.GetChild(1).Text);                            
                            string ss = "Top";
                            if (node.Text == "ASTTABLESETBOTTOMBORDER") ss = "Bottom";
                            node.Code += "Program.GetTable(O.GetString(" + node[0].Code + ")).CurRow.Set" + ss + "Border(O.GetInt(" + node[2].Code + "), O.GetInt(" + node[3].Code + "));" + G.NL;
                        }
                        break;

                    case "ASTTABLESETLEFTBORDER":
                    case "ASTTABLESETRIGHTBORDER":
                        {                            
                            bool color = false;
                            if (node.ChildrenCount() == 4)
                            {
                                color = true;
                            }                            
                            string ss = "Left";
                            if (node.Text == "ASTTABLESETRIGHTBORDER") ss = "Right";
                            if (color)
                            {
                                node.Code += "Program.GetTable(O.GetString(" + node[0].Code + ")).CurRow.Set" + ss + "Border(O.GetInt(" + node[2].Code + "), O.GetInt(" + node[3].Code + "));" + G.NL;                               
                            }
                            else
                            {
                                node.Code += "Program.GetTable(O.GetString(" + node[0].Code + ")).CurRow.Set" + ss + "Border(O.GetInt(" + node[2].Code + "));" + G.NL;                                                               
                            }
                        }
                        break;

                    case "ASTTABLEHIDELEFTBORDER":
                    case "ASTTABLEHIDERIGHTBORDER":
                        {                            
                            string ss = "Left";
                            if (node.Text == "ASTTABLEHIDERIGHTBORDER") ss = "Right";
                            node.Code += "Program.GetTable(O.GetString(" + node[0].Code + ")).CurRow.Hide" + ss + "Border(O.GetInt(" + node[2].Code + "));" + G.NL;
                        }
                        break;
                    case "ASTTABLESHOWBORDERS":
                        {                            
                            node.Code += "Program.GetTable(O.GetString(" + node[0].Code + ")).CurRow.ShowBorders();";
                        }                        
                        break;

                    case "ASTTABLESETTEXT":
                        {
                            node.Code += "Program.GetTable(O.GetString(" + node[0].Code + ")).CurRow.SetText(O.GetInt(" + node[2].Code + "), O.GetString(" + node[3].Code + "));";
                        }
                        break;

                    case "ASTTABLEALIGNLEFT":                        
                    case "ASTTABLEALIGNCENTER":                    
                    case "ASTTABLEALIGNRIGHT":
                        {
                            string type = "Left";
                            if (node.Text == "ASTTABLEALIGNCENTER") type = "Center";
                            else if (node.Text == "ASTTABLEALIGNRIGHT") type = "Right";
                            node.Code += "Program.GetTable(O.GetString(" + node[0].Code + ")).CurRow.Align" + type + "(O.GetInt(" + node[2].Code + "));";
                        }
                        break;
                    case "ASTTABLEMERGECOLS":
                        {
                            node.Code += "Program.GetTable(O.GetString(" + node[0].Code + ")).CurRow.MergeCols(O.GetInt(" + node[2].Code + "), O.GetInt(" + node[3].Code + "));";
                        }
                        break;

                    case "ASTTABLESETDATES":
                        {                            
                            node.Code += "Program.GetTable(O.GetString(" + node[0].Code + ")).CurRow.SetDates(O.GetInt(" + node[2].Code + "), O.GetDate(" + node[3].Code + "), O.GetDate(" + node[4].Code + "));";                            
                        }
                        break;


                    case "ASTTABLE":
                    case "ASTMENUTABLE":
                        {
                            node.Code += "O.Table.CallTableFile o" + Num(node) + " = new O.Table.CallTableFile();" + G.NL;
                            GetCodeFromAllChildren(node);
                            if (node.Text == "ASTMENUTABLE") node.Code += "o.menuTable = true;" + G.NL;
                            node.Code += "o" + Num(node) + ".p = p;" + G.NL;
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;                           
                        }
                        break;

                    //node.Code += "o" + Num(node) + ".name = O.GetString(" + node[0].Code + ");" + G.NL;
                    //        node.Code += "o" + Num(node) + ".col = O.GetInt(" + node[1].Code + ");" + G.NL;
                    //        node.Code += "o" + Num(node) + ".t1 = O.GetDate(" + node[2].Code + ");" + G.NL;
                    //        node.Code += "o" + Num(node) + ".t2 = O.GetDate(" + node[3].Code + ");" + G.NL;
                    //        node.Code += node[4].Code;
                    //        node.Code += "o" + Num(node) + ".printcode = O.GetString(" + node[5].Code + ");" + G.NL;
                    //        node.Code += "o" + Num(node) + ".scale = O.GetVal(" + node[6].Code + ");" + G.NL;
                    //        node.Code += "o" + Num(node) + ".format = O.GetString(" + node[7].Code + ");" + G.NL;

                    case "ASTPRTELEMENTDEC":
                        {
                            node.Code += "o" + Num(node) + ".dec = O.GetInt(" + node[0].Code + ");" + G.NL;
                        }
                        break;

                    case "ASTPRTELEMENTWIDTH":
                        {
                            node.Code += "o" + Num(node) + ".width = O.GetInt(" + node[0].Code + ");" + G.NL;
                        }
                        break;

                    case "ASTPRTELEMENTNDEC":
                        {
                            node.Code += "o" + Num(node) + ".ndec = O.GetInt(" + node[0].Code + ");" + G.NL;
                        }
                        break;

                    case "ASTPRTELEMENTNWIDTH":
                        {
                            node.Code += "o" + Num(node) + ".nwidth = O.GetInt(" + node[0].Code + ");" + G.NL;
                        }
                        break;

                    case "ASTPRTELEMENTPDEC":
                        {
                            node.Code += "o" + Num(node) + ".pdec = O.GetInt(" + node[0].Code + ");" + G.NL;
                        }
                        break;

                    case "ASTPRTELEMENTPWIDTH":
                        {
                            node.Code += "o" + Num(node) + ".pwidth = O.GetInt(" + node[0].Code + ");" + G.NL;
                        }
                        break;

                    case "ASTOLSELEMENT":
                    case "ASTPRTELEMENT":
                    case "ASTTABLESETVALUESELEMENT":
                        {
                            w.expressionCounter++;
                            node.Code += "{" + G.NL;  //avoid scope collisions                            
                            if (node.Text == "ASTOLSELEMENT" || node.Text == "ASTTABLESETVALUESELEMENT")
                            {
                                node.Code += "List<int> bankNumbers = O.Prt.CreateBankHelper(1);" + G.NL;
                            }
                            else
                            {
                                node.Code += "List<int> bankNumbers = null;" + G.NL;
                            }

                            string givenLabel = null;
                            if (node.Text != "ASTTABLESETVALUESELEMENT")
                            {                                
                                //givenLabel = w.prtLabels[w.commandLinesCounter][w.expressionCounter];
                                //if (givenLabel != null && givenLabel.Length > 2)
                                //{
                                //    givenLabel = givenLabel.Substring(1, givenLabel.Length - 2);
                                //}
                                //else
                                //{
                                //    givenLabel = w.prtItems[w.commandLinesCounter][w.expressionCounter];
                                //}

                                if (node.specialExpressionAndLabelInfo[2] != "")
                                {
                                    givenLabel = node.specialExpressionAndLabelInfo[2];
                                    givenLabel = Program.StripQuotes(givenLabel);
                                }
                                else givenLabel = node.specialExpressionAndLabelInfo[1];
                                
                            }

                            //node.Code += Globals.startListIteratorCode;
                            //TODO: what if the label is a string expression???
                            node.Code += "O.Prt.Element ope" + Num(node) + " = new O.Prt.Element();" + G.NL;  //this must be after the list start iterator code
                            node.Code += "ope" + Num(node) + ".label = O.SubstituteScalarsAndLists(`" + givenLabel + "`, false);" + G.NL;
                            ASTNode child = node.GetChild("ASTPRTELEMENTOPTIONFIELD");
                            if (child != null) node.Code += child.Code;
                            if (node.Text == "ASTPRTELEMENT")
                            {
                                node.Code += "bankNumbers = O.Prt.GetBankNumbers(null, Program.GetElementPrintCodes(o" + Num(node) + ", ope" + Num(node) + "));" + G.NL;
                            }
                            else if (node.Text == "ASTTABLESETVALUESELEMENT")
                            {
                                node.Code += "bankNumbers = O.Prt.GetBankNumbers(Globals.tableOption, new List<string>(){o" + Num(node) + ".printcode}" + ");" + G.NL;
                            }
                            node.Code += "foreach(int bankNumber in bankNumbers) {" + G.NL;  //For bankNumber = 2, no cache will ever be used to avoid confusion. Cache is only for 1 (Work).
                            //node.Code += node.CodeSentFromSubTree;  //Contains names of timeseries used deeper down the tree
                            //node.Code += "TimeSeries lhs = new TimeSeries(Program.options.freq, null);" + G.NL;
                            node.Code = EmitLocalCacheForTimeLooping(node.Code, w);
                            node.Code += "foreach (GekkoTime t2 in new GekkoTimeIterator(o" + Num(node) + ".t1.Add(-2), o" + Num(node) + ".t2))" + G.NL;
                            node.Code += Globals.startGekkoTimeIteratorCode;
                            node.Code += "O.GetVal777(" + node[0].Code + ", bankNumber, ope" + Num(node) + ", t);" + G.NL;
                            //node.Code += "O.Prt.HandlePrtElements(ope" + Num(node) + ", bankNumber, t, data);" + G.NL;                      
                            node.Code += Globals.endGekkoTimeIteratorCode;
                            //node.Code += "if (bankNumber == 1) ope" + Num(node) + ".tsWork = lhs; else ope" + Num(node) + ".tsBase = lhs;" + G.NL;                                                     
                            node.Code += "}" + G.NL;
                            node.Code += "o" + Num(node) + ".prtElements.Add(ope" + Num(node) + ");" + G.NL;
                            //node.Code += Globals.endListIteratorCode;
                            node.Code += "}" + G.NL;  //avoid scope collisions
                        }
                        break;
                    case "ASTOLSELEMENTS":
                    case "ASTPRTELEMENTS":
                        {
                            GetCodeFromAllChildren(node);
                        }
                        break;
                    case "ASTRENAME":
                        {
                            //similar to ASTCOPY
                            node.Code += "O.Rename o" + Num(node) + " = new O.Rename();" + G.NL;                            
                            //node.Code += "o" + Num(node) + ".listItems = new List<string>();" + G.NL;
                            node.Code += node[0].Code;  //list1
                            //node.Code += "o" + Num(node) + ".listItems1 = o" + Num(node) + ".listItems;" + G.NL;                            
                            //node.Code += "o" + Num(node) + ".listItems = new List<string>();" + G.NL;
                            node.Code += node[1].Code;  //list2
                            //node.Code += "o" + Num(node) + ".listItems2 = o" + Num(node) + ".listItems;" + G.NL;                            
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTRETURN":
                        {
                            //TODO TODO
                            //TODO TODO
                            //TODO TODO what about returns from cmd files...??
                            //TODO TODO and beware about the C1(), C2(), ... issue
                            //TODO TODO
                            //TODO TODO                            
                            if (w.functionHelper == null)
                            {
                                if (node.ChildrenCount() > 0)
                                {
                                    G.Writeln2("*** ERROR: RETURN with values only allowed inside FUNCTION");
                                    throw new GekkoException();
                                }
                                else
                                {
                                    //#9807235423 return problem, should it be return true?? C1(), C2(), ...
                                    node.Code += "return;" + G.NL;  //probably the node[0].Code is always empty here (should be)
                                }
                            }
                            else
                            {
                                node.Code += "return (" + G.GetVariableType(w.functionHelper.lhsTypes.Count) + ")(" + node[0].Code + ");" + G.NL;
                            }
                        }
                        break;
                    case "ASTEXPRESSIONTUPLE":  //see ASTRETURNTUPLE
                        {
                            int counter = 0;
                            List<string> types = new List<string>();

                            string classCs = G.GetVariableType(node.ChildrenCount());
                            CreateTupleClass(w.headerCs, node.ChildrenCount(), classCs, w.tupleClasses);

                            string tempCs = "temp" + ++Globals.counter;                            
                                                        
                            string s = null;
                            foreach (ASTNode child in node.ChildrenIterator())
                            {
                                //counter++;                                
                                //node.Code += tempCs + ".tuple" + (counter - 1) + " = " + child.Code + ";" + G.NL;
                                s += child.Code + ", ";
                            }
                            if (s.EndsWith(", ")) s = s.Substring(0, s.Length - 2);

                            node.Code = "(new " + classCs + "(" + s + "))";

                            //node.Code += classCs + " " + tempCs + " = new " + classCs + "(" + s + ");" + G.NL;
                            //node.Code += "return " + tempCs + ";" + G.NL;
                        }
                        break;
                    case "ASTRETURNTUPLE":  //see ASTRETURN
                        {                            
                            //TODO TODO and beware about the C1(), C2(), ... issue                                            
                            if (w.functionHelper == null)
                            {
                                if (node[0].Code != null)
                                {
                                    G.Writeln2("*** ERROR: RETURN with multiple values only allowed inside FUNCTION");
                                    throw new GekkoException();
                                }                                
                            }
                            else
                            {
                                
                                
                                
                                
//o0.t1 = Globals.globalPeriodStart;
//o0.t2 = Globals.globalPeriodEnd;
//o0.lhs = O.GetTimeSeries(O.GetTimeSeriesFromCache(ref iv3, "y", 1));
//foreach (GekkoTime t in new 
// GekkoTimeIterator(o0.t1, o0.t2))
//{
//  Globals.globalGekkoTimeIterator_DO_NOT_ALTER = t; 
//  double data = O.GetVal(O.GetTimeSeriesFromCache(ref iv4, "x", 1));
//  o0.lhs.SetData(t, data);
//}
//Globals.globalGekkoTimeIterator_DO_NOT_ALTER = Globals.tNull;



                                if (w.functionHelper.lhsTypes.Count != node.ChildrenCount())
                                {
                                    G.Writeln2("*** ERROR: Return with " + node.ChildrenCount() + " items instead of " + w.functionHelper.lhsTypes.Count);
                                    throw new GekkoException();
                                }
                                
                                string tempCs = "temp" + ++Globals.counter;
                                string classCs =  G.GetVariableType(w.functionHelper.lhsTypes.Count);

                                string s = null;
                                string s2 = null;
                                int counter = -1;
                                foreach (ASTNode child in node.ChildrenIterator())
                                {
                                    counter++;
                                    string tn = "temp" + ++Globals.counter;
                                    
                                    if (w.functionHelper.lhsTypes[counter] == "series")
                                    {
                                        string tempName = "temp" + ++Globals.counter;
                                        s += "TimeSeries " + tempName + " = new TimeSeries(Program.options.freq, null);" + G.NL;
                                        s += "foreach (GekkoTime t2 in new GekkoTimeIterator(Globals.globalPeriodStart, Globals.globalPeriodEnd))" + G.NL;
                                        s += "{" + G.NL;
                                        s += "    t = t2;" + G.NL;
                                        s += "    double data = O.GetVal(" + child.Code + ", t);" + G.NL;
                                        s += "    " + tempName + ".SetData(t, data);" + G.NL;
                                        s += "}" + G.NL;
                                        s += "t = Globals.tNull;" + G.NL;
                                        s += "IVariable " + tn + " = new MetaTimeSeries(" + tempName + ");" + G.NL;

                                    }
                                    else
                                    {
                                        s += "IVariable " + tn + " = " + child.Code + ";" + G.NL;                                        
                                    }
                                    s2 += tn + ", ";
                                }
                                if (s2.EndsWith(", ")) s2 = s2.Substring(0, s2.Length - 2);

                                node.Code += s + classCs + " " + tempCs + " = new " + classCs + "(" + s2 + ");" + G.NL;

                                if (node.ChildrenCount() != w.functionHelper.lhsTypes.Count)
                                {
                                    G.Writeln2("*** ERROR: RETURN with " + node.ChildrenCount() + " values encountered in '" + w.functionHelper.functionName + "' function");
                                    throw new GekkoException();
                                }                               
                                
                                node.Code += "return " + tempCs + ";" + G.NL;
                            }
                        }
                        break;
                    case "ASTSIGN":
                        {
                            node.Code += "Program.Sign();" + G.NL;
                        }
                        break;
                    case "ASTSIM":
                        {
                            node.Code += "O.Sim o" + Num(node) + " = new O.Sim();" + G.NL;
                            //node.Code += "o" + Num(node) + ".t1 = G.GetStartDate(o" + Num(node) + ".t1);" + G.NL;
                            //node.Code += "o" + Num(node) + ".t2 = G.GetEndDate(o" + Num(node) + ".t2);" + G.NL;
                            GetCodeFromAllChildren(node);  //gets dates and options
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTRES":
                        {
                            node.Code += "O.Res o" + Num(node) + " = new O.Res();" + G.NL;                            
                            GetCodeFromAllChildren(node);  //gets dates
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTSMOOTH":
                        {
                            node.Code += "O.Smooth o" + Num(node) + " = new O.Smooth();" + G.NL;
                            node.Code += "o" + Num(node) + ".p = p;" + G.NL;
                            //node.Code += "o" + Num(node) + ".lhs = (" + node[0].Code + ");" + G.NL;
                            //node.Code += "o" + Num(node) + ".rhs = (" + node[1].Code + ");" + G.NL;
                            //for (int i = 2; i < node.ChildrenCount(); i++)  //get the rest
                            //{
                            //    node.Code += node[i].Code;
                            //}
                            GetCodeFromAllChildren(node);
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTSPLICE":
                        {
                            node.Code += "O.Splice o" + Num(node) + " = new O.Splice();" + G.NL;
                            node.Code += node[0].Code;
                            node.Code += node[1].Code;
                            node.Code += node[2].Code;
                            //node.Code += "o" + Num(node) + ".rhs1 = (" + node[1].Code + ");" + G.NL;
                            //node.Code += "o" + Num(node) + ".rhs2 = (" + node[2].Code + ");" + G.NL;
                            if (node.ChildrenCount() > 3)
                            {
                                node.Code += "o" + Num(node) + ".date = O.GetDate(" + node[3].Code + ");" + G.NL;
                            }
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;

                        }
                        break;
                    case "ASTTIME":
                        {
                            node.Code += "O.Time o" + Num(node) + " = new O.Time();" + G.NL;
                            GetCodeFromAllChildren(node);
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTTIMEFILTER":
                        {
                            node.Code += "O.TimeFilter " + "o" + Num(node) + " = new O.TimeFilter();" + G.NL;
                            GetCodeFromAllChildren(node);
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTTIMEFILTERPERIODS":
                        {
                            GetCodeFromAllChildren(node);
                        }
                        break;
                    case "ASTTIMEFILTERPERIOD":
                        {
                            string tempName = "temp" + ++Globals.counter;
                            node.Code += "O.TimeFilterHelper " + tempName + " = new O.TimeFilterHelper();" + G.NL;
                            if (node.ChildrenCount() == 1)
                            {                                
                                node.Code += tempName + ".from = O.GetDate(" + node[0].Code + ");" + G.NL;                                                                
                            }
                            else if (node.ChildrenCount() == 2) {
                                node.Code += tempName + ".from = O.GetDate(" + node[0].Code + ");" + G.NL;
                                node.Code += tempName + ".to = O.GetDate(" + node[1].Code + ");" + G.NL;                                                                
                            }
                            else if (node.ChildrenCount() == 3) {
                                node.Code += tempName + ".from = O.GetDate(" + node[0].Code + ");" + G.NL;
                                node.Code += tempName + ".to = O.GetDate(" + node[1].Code + ");" + G.NL;
                                node.Code += tempName + ".step = O.GetInt(" + node[2].Code + ");" + G.NL;
                            }
                            node.Code += "o" + Num(node) + ".timeFilterPeriods.Add(" + tempName + ");" + G.NL;
                        }
                        break;
                    case "ASTQ":
                        {
                            node.Code += AddPrintCode("q", node[0].Code, node.Parent.Parent.Text, node);
                        }
                        break;
                    case "ASTTRIMVARS":
                        {
                            node.Code += "Program.Trimvars();" + G.NL;
                        }
                        break;

                    case "ASTDECOMP":
                        {
                            //string cs = helper.decompExpressionCs.Replace("`", "\\\"");
                            //cs = cs.Replace(G.NL, "");
                            //node.Code += "Program.Decomp(`" + helper.decompType.ToLower() + "`, " + helper.timePeriod + ", `" + helper.prtOption + "`, `" + cs + "`, precedents, expression);");                            
                            node.Code += "O.Decomp o" + Num(node) + " = new O.Decomp();" + G.NL;
                            GetCodeFromAllChildren(node);
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;

                        }
                        break;
                    case "ASTDECOMPITEMS":
                        {                            
                            ASTNode child = node[0];
                            if (child.Text == "ASTNAMEWITHBANK")
                            {
                                //Same logic as ASTLISTITEMS, where "LIST a" is parsed as an expression but translated into 'a' in the parser.
                                node.Code += "o" + Num(node) + ".variable = O.GetString(" + AstBankHelper(child, w, 1) + ");" + G.NL;
                            }
                            else
                            {
                                G.Writeln2("*** ERROR: Sorry, decomposition of expressions not yet implemented in Gekko 2.0");
                                throw new GekkoException();
                                node.Code += "o" + Num(node) + ".expressionCs = " + child.Code + ";" + G.NL;
                            }                            
                        }
                        break;
                    case "ASTUNFIX":
                        node.Code += "Program.Unfix();" + G.NL;
                        break;
                    case "ASTUNSWAP":
                        {
                            node.Code += "Program.Unswap();" + G.NL;
                        }
                        break;
                    case "ASTVERS":
                        {
                            node.Code += "Program.Vers();" + G.NL;
                        }
                        break;
                    case "ASTUPDPRT":
                        {
                            node.Code += "O.Updprt o" + Num(node) + " = new O.Updprt();" + G.NL;                            
                            node.Code += node[0].Code;
                            node.Code += node[1].Code;
                            node.Code += "o" + Num(node) + ".op = `" + node[2].Code + "`;" + G.NL;                            
                            if (node.ChildrenCount() > 3)
                            {
                                node.Code += node[3].Code;
                            }
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTPIPE":
                        {                            
                            node.Code += "O.Pipe o" + Num(node) + " = new O.Pipe();" + G.NL;                            
                            GetCodeFromAllChildren(node);  //gets fileName and options
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTREAD":
                        {
                            node.Code += Globals.clearTsCsCode + G.NL;
                            node.Code += "O.Read o" + Num(node) + " = new O.Read();" + G.NL;
                            node.Code += "o" + Num(node) + ".p = p;";
                            GetCodeFromAllChildren(node);
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTREADTO":
                        {
                            if (node.ChildrenCount() > 0)
                            {
                                node.Code += "o" + Num(node) + ".readTo = `" + node[0].Text + "`;";
                            }                            
                        }
                        break;
                    case "ASTIMPORTTO":
                        {
                            if (node.ChildrenCount() > 0)
                            {
                                node.Code += "o" + Num(node) + ".importTo = `" + node[0].Text + "`;";
                            }
                        }
                        break;
                    case "ASTWRITE":
                        {                            
                            node.Code += "O.Write o" + Num(node) + " = new O.Write();" + G.NL;                            
                            GetCodeFromAllChildren(node);
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTX12A":
                        {
                            node.Code += "O.X12a o" + Num(node) + " = new O.X12a();" + G.NL;
                            GetCodeFromAllChildren(node);
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTR_FILE":
                        {
                            node.Code += "O.R_file o" + Num(node) + " = new O.R_file();" + G.NL;
                            GetCodeFromAllChildren(node);
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTR_EXPORT":
                        {
                            node.Code += "O.R_export o" + Num(node) + " = new O.R_export();" + G.NL;
                            GetCodeFromAllChildren(node);
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTR_RUN":
                        {
                            node.Code += "O.R_run o" + Num(node) + " = new O.R_run();" + G.NL;
                            GetCodeFromAllChildren(node);
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTR_EXPORTITEMS":
                        {
                            //G.Writeln2(node.Text);                            
                            string s2 = "";
                            bool first = true;
                            foreach (ASTNode child in node.ChildrenIterator())
                            {
                                if (first) s2 += "`" + child.Text + "`";
                                else s2 += ", `" + child.Text + "`";
                                first = false;
                            }
                            node.Code += "o" + Num(node) + ".r_exportItems = new List<string>() {" + s2 + "};" + G.NL;
                        }
                        break;
                    case "ASTTRANSLATE":
                        {
                            node.Code += "O.Translate o" + Num(node) + " = new O.Translate();" + G.NL;
                            GetCodeFromAllChildren(node);
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTEXPORT":
                        {
                            node.Code += "O.Export o" + Num(node) + " = new O.Export();" + G.NL;
                            GetCodeFromAllChildren(node);
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTFILENAMESTAR":
                        {
                            node.Code = "new ScalarString(`*`)";
                        }
                        break;
                    case "ASTHANDLEFILENAME":
                        {
                            if (node.ChildrenCount() > 0)
                            {
                                node.Code += "o" + Num(node) + ".fileName = O.GetString(" + node[0].Code + ");" + G.NL;
                            }
                        }
                        break;                    
                    case "ASTRUN":
                        {
                            node.Code += "O.Run o" + Num(node) + " = new O.Run();" + G.NL;
                            //HMMM is this right:
                            node.Code += "o" + Num(node) + ".fileName = " + node[0].Code + ".GetString();" + G.NL;
                            node.Code += "o" + Num(node) + ".p = p;" + G.NL;
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTSCALAR":
                        {
                            HandleScalar(node, false, w);                            
                        }
                        break;
                    case "ASTSTAMP":
                        {
                            node.Code += "Program.Stamp();" + G.NL;
                        }
                        break;
                    case "ASTSTRINGINQUOTES":
                        {
                            string s = Program.StripQuotes(node[0].Text);
                            //for instance, @"this is a ""word"" shown", where "" are kind of @-escaped.
                            //but @ will keep backslashes.
                            s = s.Replace("\"", "\"\"");
                            node.Code = "new ScalarString(@`" + s + "`)";                            
                        }
                        break;
                    case "ASTSTRING":
                    case "ASTNAME2":
                        {
                            if (node[0].Text == "?")
                            {
                                if (node.ChildrenCount() > 1)
                                {
                                    node.Code += "O.String2.Q(`" + node[1].Text + "`);" + G.NL;
                                }
                                else
                                {
                                    if (node.Text == "ASTNAME2") node.Code += "Program.Mem(`name`);" + G.NL;
                                    else node.Code += "Program.Mem(`string`);" + G.NL;
                                }
                            }
                            else
                            {
                                bool x = false;
                                if (node.Text == "ASTNAME2") x = true;
                                string nodeCode = HandleString(node, node[1].Code, x);
                                node.Code += nodeCode;
                            }
                        }
                        break;
                    case "ASTTRUNCATE":
                        {
                            node.Code += "O.Truncate o" + Num(node) + " = new O.Truncate();" + G.NL;
                            node.Code += node[0].Code;  //options
                            //node.Code += "o" + Num(node) + ".listItems = new List<string>();" + G.NL;
                            node.Code += node[1].Code;  //list1                            
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTCOMPARECOMMAND":
                        {
                            node.Code += "O.Compare o" + Num(node) + " = new O.Compare();" + G.NL;                            
                            GetCodeFromAllChildren(node);
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTDISP":
                        {                            
                            node.Code += "O.Disp o" + Num(node) + " = new O.Disp();" + G.NL;                            
                            node.Code += node[0].Code;  //dates
                            node.Code += node[1].Code;  //list                            
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;                            
                        }
                        break;
                    case "ASTINI":
                        {
                            node.Code += "Program.Ini(p);";
                        }
                        break;
                    case "ASTINFO":
                        {
                            node.Code += "O.Info o" + Num(node) + " = new O.Info();" + G.NL;
                            node.Code += node[0].Code;  //dates
                            node.Code += node[1].Code;  //list                            
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;

                    case "ASTITERSHOW":
                        {
                            node.Code += "O.Itershow o" + Num(node) + " = new O.Itershow();" + G.NL;
                            node.Code += node[0].Code;  //dates
                            node.Code += node[1].Code;  //list                            
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTDISPSEARCH":
                        {
                            node.Code += "O.Disp o" + Num(node) + " = new O.Disp();" + G.NL;
                            node.Code += "o" + Num(node) + ".searchName = `" + Program.StripQuotes(node[0].Text) + "`;" + G.NL;                            
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTPAUSE":
                        {                            
                            if (node.ChildrenCount() == 0) node.Code += "Program.Pause(``);";
                            else node.Code += "Program.Pause(O.GetString(" + node[0].Code + "));";
                            break;
                        }
                        break;
                    case "ASTEFTER":
                        {
                            node.Code += "O.Efter o" + Num(node) + " = new O.Efter();" + G.NL;
                            node.Code += node[0].Code;  //dates                            
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTUPD":
                        {
                            //TODO
                            //TODO hmm what about $ operator??
                            //TODO and what about 'xx' prefix? Maybe allow this for GENR too!
                            //TODO
                            node.Code += "O.Upd o" + Num(node) + " = new O.Upd();" + G.NL;
                            //node.Code += "int bankNumber = 1;" + G.NL;
                            //string bank = "new ScalarString(`Work`)";
                            //string variable = node[0].Code;
                            node.Code += node[0].Code;  //listItems
                            node.Code += node[1].Code;  //dates
                            string op = node[2].Code;
                            int nn = 3;
                            int n = node.ChildrenCount() - nn;
                            //node.Code += "o" + Num(node) + ".lhs = O.GetTimeSeries(" + node[0].Code + ");" + G.NL;                            

                            //HandleLeftSideTimeSeries(node, wh2, bank, variable);
                            //node.Code += node.CodeSentFromSubTree;  //timeseries used deeper down the tree
                            node.Code += "o" + Num(node) + ".op = " + op + ";" + G.NL;
                            node.Code += "o" + Num(node) + ".data = new double[" + n + "];" + G.NL;
                            node.Code += "o" + Num(node) + ".rep = new double[" + n + "];" + G.NL;
                            for (int i = 0; i < n; i++)
                            {
                                node.Code += "o" + Num(node) + ".data[" + i + "] = (" + node[i + nn][0].Code + ").GetVal(t);" + G.NL;

                                string repCs = "new ScalarVal(1d)";
                                bool one = false;
                                if (node[i + nn].ChildrenCount() > 1)
                                {
                                    ASTNode rep = node[i + nn][1];
                                    repCs = node[i + nn][1].Code;
                                    if (rep.Text == "ASTSTAR")
                                    {
                                        repCs = "new ScalarVal(-12345d)";  //secret code for '*'                                        
                                    }
                                    else if (rep.Text == "ASTEMPTY")
                                    {
                                        one = true;
                                    }
                                }
                                else
                                {
                                    one = true;
                                }
                                if (one)
                                {
                                    node.Code += "o" + Num(node) + ".rep[" + i + "] = 1d;" + G.NL;
                                }
                                else
                                {
                                    node.Code += "o" + Num(node) + ".rep[" + i + "] = (" + repCs + ").GetVal(t);" + G.NL;
                                }
                            }
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                            //G.Writeln(node.Code);
                        }
                        break;
                    case "ASTUPDOPERATOR":
                        {
                            node.Code += "`" + node[0].Code + "`";
                        }
                        break;
                    case "ASTVAL":
                        {
                            if (node[0].Text == "?")
                            {
                                if (node.ChildrenCount() > 1)
                                {
                                    node.Code += "O.Val.Q(`" + node[1].Text + "`);" + G.NL;
                                }
                                else
                                {
                                    node.Code += "Program.Mem(`val`);" + G.NL;
                                }
                            }
                            else
                            {
                                node.Code += HandleVal(node, node[1].Code, w);
                            }
                        }
                        break;
                    case "ASTSHOW":
                        {
                            string givenLabel = null;
                            if (node.specialExpressionAndLabelInfo[2] != "")
                            {
                                givenLabel = node.specialExpressionAndLabelInfo[2];
                                givenLabel = Program.StripQuotes(givenLabel);
                            }
                            else givenLabel = node.specialExpressionAndLabelInfo[1];                            
                            
                            node.Code += "O.Show o" + Num(node) + " = new O.Show();" + G.NL;

                            node.Code += "o" + Num(node) + ".input = " + node[0].Code + ";" + G.NL;
                            node.Code += "o" + Num(node) + ".label = @`" + givenLabel + "`;" + G.NL;
                            node.Code += "o" + Num(node) + ".Exe();" + G.NL;
                        }
                        break;
                    case "ASTTUPLE":
                        {
                            string rhsCode = node[1].Code;

                            if (node[1].Text == "ASTFUNCTION" && (G.equal(node[1][0].Text.ToLower(), "laspchain") || G.equal(node[1][0].Text.ToLower(), "laspfixed")))
                            {
                                //hack to make it work. Problem is that method cannot run year-by-year.
                                if (node[0].ChildrenCount() != 2)
                                {
                                    G.Writeln2("laspchain() and laspfixed() must be called with (series x, series y) on left side");
                                    throw new GekkoException();
                                }
                                //string cs1 = "IVariable p" + Num(node) + "= " + node[0][0][2].Code + ";";
                                //string cs2 = "IVariable q" + Num(node) + " = " + node[0][1][2].Code + ";";
                                //string cs3 = "GekkoTuple.Tuple2 temp = " + rhsCode + ";";
                                node.Code += "Functions.HandleLasp(" + rhsCode + ", " + node[0][0][2].Code + ", " + node[0][1][2].Code + ");" + G.NL;                            
                            }
                            else
                            {


                                string tempName = "temp" + ++Globals.counter;
                                string nodeCodeTemp = null;
                                int number = 0;
                                foreach (ASTNode child in node[0].ChildrenIterator())
                                {
                                    if (child.Text != "ASTTUPLEITEM")
                                    {
                                        G.Writeln("*** ERROR #74343641");
                                        throw new GekkoException();
                                    }
                                    number++;

                                    if (child[0].Text == "val")
                                    {
                                        nodeCodeTemp += HandleVal(child[1], tempName + ".tuple" + (number - 1), w);
                                    }
                                    else if (child[0].Text == "date")
                                    {
                                        nodeCodeTemp += HandleDate(child[1], tempName + ".tuple" + (number - 1));
                                    }
                                    else if (child[0].Text == "name")
                                    {
                                        nodeCodeTemp += HandleString(child[1], tempName + ".tuple" + (number - 1), true);
                                    }
                                    else if (child[0].Text == "string")
                                    {
                                        nodeCodeTemp += HandleString(child[1], tempName + ".tuple" + (number - 1), false);
                                    }
                                    else if (child[0].Text == "series")
                                    {
                                        ClearLocalStatementCache(w);
                                        nodeCodeTemp += HandleGenr(Num(child), child[1].Code, child[2].Code, tempName + ".tuple" + (number - 1), w, null);
                                    }
                                    else if (child[0].Text == "list")
                                    {
                                        string s = "o" + Num(child[1]) + ".listItems.AddRange(O.GetList(" + tempName + ".tuple" + (number - 1) + "));" + G.NL;
                                        nodeCodeTemp += HandleList(child[1], s);
                                    }
                                }

                                string className = G.GetVariableType(number);
                                node.Code += className + " " + tempName + " = " + rhsCode + ";" + G.NL;  //for instance "ScalarVal_ScalarVal temp117 = f()"                            
                                node.Code += nodeCodeTemp;
                            }
                            
                        }
                        break;
                    case "ASTYES":
                        {
                            node.Code += "new ScalarString(`yes`)";
                        }
                        break;
                    case "ASTWILDCARD":
                        {
                            bool first = true;
                            string s = null;
                            foreach (ASTNode child in node.ChildrenIterator())
                            {
                                if (first) s = child.Code;
                                else s += ".Add(" + child.Code + ", t)";
                                first = false;
                            }
                            //node.Code = "O.IndexerAlone(" + s + ")";
                            node.Code = s;
                        }
                        break;
                    case "ASTWILDSTAR":
                        {
                            node.Code = "new ScalarString(`*`)";
                        }
                        break;
                    case "ASTWILDQUESTION":
                        {
                            node.Code = "new ScalarString(`?`)";
                        }
                        break;
                    case "ASTUPDOPERATOREQUAL":
                        {
                            node.Code = "=";
                        }
                        break;
                    case "ASTUPDOPERATORSTAR":
                        {
                            node.Code = "*";
                        }
                        break;
                    case "ASTUPDOPERATORHAT":
                        {
                            node.Code = "^";
                        }
                        break;
                    case "ASTUPDOPERATORPLUS":
                        {
                            node.Code = "+";
                        }
                        break;
                    case "ASTUPDOPERATORHASH":
                        {
                            node.Code = "#";
                        }
                        break;
                    case "ASTUPDOPERATORPERCENT":
                        {
                            node.Code = "%";
                        }
                        break;
                    case "ASTUPDOPERATOREQUALDOLLAR":
                        {
                            node.Code = "=$";
                        }
                        break;
                    case "ASTUPDOPERATORSTARDOLLAR":
                        {
                            node.Code = "*$";
                        }
                        break;
                    case "ASTUPDOPERATORHATDOLLAR":
                        {
                            node.Code = "^$";
                        }
                        break;
                    case "ASTUPDOPERATORPLUSDOLLAR":
                        {
                            node.Code = "+$";
                        }
                        break;
                    case "ASTUPDOPERATORHASHDOLLAR":
                        {
                            node.Code = "#$";
                        }
                        break;
                    case "ASTUPDOPERATORPERCENTDOLLAR":
                        {
                            node.Code = "%$";
                        }
                        break;                   

                }
            }

            if (relativeDepth == 1)
            {

                //#982375: if it is 0, walk the sub-tree to see...
                if (!w.wh.isGotoOrTarget)
                {
                    node.Code = "p.SetText(@`¤" + node.Line + "`);" + G.NL //so errors get line numbers
                        //+ "Globals.existErrors = null;" + G.NL //resetting this, used in SERIES and PRT/PPLOT/etc.
                        + node.Code + G.NL;
                }
            }

            if (node != null && absoluteDepth == 0)
            {
                //returning from a Gekko statement
                //This can be a statement with several Gekko commands
                foreach (ASTNode child in node.ChildrenIterator())
                {
                    //if (!w.wh.isGotoOrTarget) node.Code += "{" + G.NL;                    
                    node.Code += child.Code + G.NL;
                    //if (!w.wh.isGotoOrTarget) node.Code += "}" + G.NL;                    
                }
            }
        }

        private static void AstListHelper(ASTNode node, W w, string simpleIdent, bool stringify)
        {
            node.Code = null; //clearing
            string stringifyString = "false"; if (stringify) stringifyString = "true";
            string s = null; w.listCache.TryGetValue(simpleIdent, out s);
            if (s == null)
            {
                //has not been seen before
                string listWithNumber = "list" + ++Globals.counter;
                w.listCache.Add(simpleIdent, listWithNumber);
                w.headerCs.AppendLine("public static IVariable " + listWithNumber + " = null;");  //cannot set it to ScalarVal since it may change type...
                node.Code += "O.GetScalarFromCache(ref " + listWithNumber + ", `" + Globals.symbolList + simpleIdent + "`, false, " + stringifyString + ")";
            }
            else
            {
                node.Code += "O.GetScalarFromCache(ref " + s + ", `" + Globals.symbolList + simpleIdent + "`, false, " + stringifyString + ")";
            }
        }

        private static void ClearLocalStatementCache(W w)
        {
            w.wh.localStatementCache = new GekkoDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        private static string HandleList(ASTNode node, string childCode)
        {
            string nodeCode = null;
            nodeCode += "O.List o" + Num(node) + " = new O.List();" + G.NL;
            if (node[0].Text == "ASTLISTFILE")
            {
                nodeCode += "o" + Num(node) + ".listFile = O.GetString(" + node[0].Code + ");" + G.NL;
            }
            else
            {
                nodeCode += "o" + Num(node) + ".name = O.GetString(" + node[0].Code + ");" + G.NL;
            }
            nodeCode += "o" + Num(node) + ".listItems = new List<string>();" + G.NL;

            //Quite an ugly hack. Problem is that with "LIST<direct>xx = 0, 1, 2" Gekko will try to convert 0 into a list and will
            //throw an exception before O.List.Exe() is even called. So with <direct> we need to suppress the O.GetList()-code that 
            //puts stuff inside the elements.
            bool directOption = false;
            string extraCode = null;
            for (int i = 2; i < node.ChildrenCount(); i++)
            {
                if (node[i].Text == "ASTDIRECT") directOption = true;
                extraCode += node[i].Code;
            }
            if (directOption)
            {
                nodeCode += "o" + Num(node) + ".direct = true;" + G.NL;
                nodeCode += "o" + Num(node) + ".rawfood = " + "@`" + node.specialExpressionAndLabelInfo[1] + "`" + ";" + G.NL;
            }
            else
            {
                nodeCode += childCode;
            }
            nodeCode += extraCode;
            
            nodeCode += "o" + Num(node) + ".Exe();" + G.NL;
            return nodeCode;
        }

        private static string HandleString(ASTNode node, string childCode, bool isName)
        {
            string x = "false";
            if (isName) x = "true";

            string nodeCode = "O.SetStringData(" + node[0].Code + ", " + childCode + ", " + x + ");" + G.NL;
            return nodeCode;
        }

        private static string HandleDate(ASTNode node, string childCode)
        {
            string nodeCode = "O.SetDateData(" + node[0].Code + ", " + childCode + ");" + G.NL;
            return nodeCode;
        }

        private static string HandleVal(ASTNode node, string childCode, W w)
        {
            string nodeCodeTemp = null;            
            ASTNode node0 = node[0];
            if (node[0].Text == "?")
            {
                nodeCodeTemp = "O.Val.Q(" + Globals.QT + node[1].Text + Globals.QT + ");" + G.NL;
            }
            else
            {
                if (node0.nameSimpleIdent != null)
                {
                    //It is a simple ident code, such as VAL x = ...                                
                    string tempDoubleCs = "tempDouble" + ++Globals.counter;
                    nodeCodeTemp += "double " + tempDoubleCs + " = (" + childCode + ").GetVal(t);" + G.NL;
                    string notUsed = null;
                    string leftSideCs = CacheRefScalarCs(out notUsed, node0.nameSimpleIdent, w.scalarCache, w.headerCs, EScalarRefType.Val, tempDoubleCs, false, true, false);
                    nodeCodeTemp += leftSideCs + G.NL;
                }
                else
                {
                    //fancy name like VAL x|%y = ... --> this will be slow!                                
                    nodeCodeTemp += "O.SetValData(" + node0.Code + ", " + childCode + ");" + G.NL;
                }
            }
            return nodeCodeTemp;
        }

        private static string HandleGenr(string numNode, string childCodePeriod, string childCodeLhsName, string childCodeRhs, W w, string lhsFunction)
        {
            string nodeCode = null;
            nodeCode += "O.Genr o" + numNode + " = new O.Genr();" + G.NL;
            nodeCode = EmitLocalCacheForTimeLooping(nodeCode, w);
            nodeCode += childCodePeriod + G.NL;  //dates
            nodeCode += "o" + numNode + ".lhs = null;";
            nodeCode += "foreach (GekkoTime t2 in new GekkoTimeIterator(o" + numNode + ".t1, o" + numNode + ".t2))" + G.NL;
            nodeCode += Globals.startGekkoTimeIteratorCode;
            nodeCode += "  double data = O.GetVal(" + childCodeRhs + ", t);" + G.NL;            
            nodeCode += "if(o" + numNode + ".lhs == null) o" + numNode + ".lhs = O.GetTimeSeries(" + childCodeLhsName + ");" + G.NL; //we want the rhs to be constructed first, so that SERIES xx1 = xx1; fails if y does not exist (otherwist it would have been autocreated).                        
            //nodeCode += "  double dataLag = O.GetVal(o" + numNode + ".lhs, t.Add(-1));" + G.NL;
            if (lhsFunction == null)
            {
                nodeCode += "o" + numNode + ".lhs.SetData(t, data);" + G.NL;
            }
            else if (G.equal(lhsFunction, "log"))
            {
                nodeCode += "o" + numNode + ".lhs.SetData(t, Math.Exp(data));" + G.NL;
            }
            else if (G.equal(lhsFunction, "dlog"))
            {
                nodeCode += "o" + numNode + ".lhs.SetData(t, o" + numNode + ".lhs.GetData(t.Add(-1)) * Math.Exp(data));" + G.NL;
            }
            else if (G.equal(lhsFunction, "pch"))
            {
                nodeCode += "o" + numNode + ".lhs.SetData(t, o" + numNode + ".lhs.GetData(t.Add(-1)) * (data/100d + 1));" + G.NL;
            }
            else if (G.equal(lhsFunction, "dif") || G.equal(lhsFunction, "diff"))
            {
                nodeCode += "o" + numNode + ".lhs.SetData(t, o" + numNode + ".lhs.GetData(t.Add(-1)) + data);" + G.NL;
            }
            else
            {
                G.Writeln2("*** ERROR: Left-hand side function '" + lhsFunction + "' is not recognized");
                G.Writeln("           Legal functions are log, dlog, pch, dif or diff");
                throw new GekkoException();
            }                        
            nodeCode += Globals.endGekkoTimeIteratorCode;
            nodeCode += "o" + numNode + ".Exe();" + G.NL;
            return nodeCode;
        }

        private static void CreateTupleClass(StringBuilder headerCs, int number, string className, GekkoDictionary<string, bool> tupleClasses)
        {
            //Tuples 2-10 are now predefined.

            //if(tupleClasses.ContainsKey(className)) return;  //do not duplicate, has been encountered before/elsewhere            
            ////a bit inefficient, but oh well
            //string ss = null;
            //string uu = null;
            //string vv = null;
            //int count = 0;
            //for (int i = 0; i < number; i++)
            //{
            //    count++;
            //    ss += "public IVariable tuple" + (count - 1) + ";" + G.NL;
            //    uu += "tuple" + (count - 1) + " = ptuble" + (count - 1) + ";" + G.NL;
            //    vv += "IVariable ptuble" + (count - 1) + ", ";
            //}
            //if (vv.EndsWith(", ")) vv = vv.Substring(0, vv.Length - 2);
            //headerCs.AppendLine("public class " + className + " { " + ss + G.NL + "public " + className + "(" + vv + ") {" + G.NL + uu + "} }");
            //tupleClasses.Add(className, true);
        }        
        
        private static string Num(ASTNode node)
        {
            return "" + node.commandLinesCounter;
        }        

        private static string EmitLocalCacheForTimeLooping(string s, W wh2)
        {
            StringBuilder sb = null;
            if (wh2.wh != null && wh2.wh.localStatementCache != null && wh2.wh.localStatementCache.Count > 0)
            {
                sb = new StringBuilder();
                foreach (KeyValuePair<string, string> kvp in wh2.wh.localStatementCache)
                {
                    sb.AppendLine("IVariable " + kvp.Value + " = " + kvp.Key + ";");
                }
                return s + sb.ToString();
            }
            return s;
        }

        private static void HandleScalar(ASTNode node, bool isCurlyWithoutPercent, W w)
        {
            bool stringify = false;
            if (node.ChildrenCount() > 0 && (node[0].Text == "ASTDOLLARPERCENTNAMESIMPLE" || node[0].Text == "ASTDOLLARPERCENTPAREN")) stringify = true;
            
            bool transformationAllowed = true;
            bool isPartOfComposedName = false;
            
            if ((node.Number == 1 && node.Parent.Text == "ASTNAMEWITHBANK") 
                || node.Parent.Text == "ASTNAME"
                || node.Parent.Text == "ASTCURLY"
                || node.Parent.Text == "ASTCURLYSIMPLE")
            {
                //For instance base:%s. If %s is NAME 'fy', this would be equal to base:fy.                            
                //In that case, O.GetScalar must not be allowed to transform the string/name into a timeseries,
                //because we are going to look up the timeseries in the databank (in the example: base databank).
                //Therefore we call an overload of O.GetScalar()
                transformationAllowed = false;
            }

            if ((node.Parent.Text == "ASTNAME" && node.Parent.ChildrenCount() > 1)
                || (node.Parent.Text == "ASTCURLY" && node.Parent.ChildrenCount() == 1))
            {
                isPartOfComposedName = true; //composed names cannot be looked up in cache                                            
            }
            
            string scalarSimpleIdent = null;
            if (isCurlyWithoutPercent)
            {
                // {s}
                scalarSimpleIdent = node.Text;
            }
            else
            {
                if (node[0].Text == "ASTPERCENTNAMESIMPLE" || node[0].Text == "ASTDOLLARPERCENTNAMESIMPLE")
                {
                    // %s, not %(...), the %s may be inside {} like {%s}
                    scalarSimpleIdent = node[0][0].Text;
                }
            }

            if (scalarSimpleIdent != null)
            {
                //either {s} or %s
                string fa = FindFunctionArguments(node, w, scalarSimpleIdent);
                if (fa != null)
                {
                    node.Code += fa;  //????????? What is this????????
                }
                else
                {                   

                    //hmmm why do we have isPartOfComposedName and transformationAllowed at the same time
                    //is it not the same thing?

                    if (!isPartOfComposedName && w.wh.localStatementCache != null)
                    {
                        //not for instance a%s or %(%s) but clean %s, and part of GENR statement                        
                        //In that case, we look for the variable in the local GENR cache
                        string refCode = "ts" + ++Globals.counter;

                        string fallBackCode = null;

                        string t = "false";
                        if (transformationAllowed) t = "true";
                        string s = "false";
                        if (stringify) s = "true";

                        fallBackCode = "O.GetScalar(`" + scalarSimpleIdent + "`, " + t + ", " + s + ")";
                        
                        string xx = null; w.wh.localStatementCache.TryGetValue(fallBackCode, out xx);
                        if (xx != null)
                        {
                            //This complicated timeseries (or scalar) has been seen before in this particular GENR statement                        
                            node.Code = xx;
                        }
                        else
                        {
                            //has not been seen before
                            w.wh.localStatementCache.Add(fallBackCode, refCode);
                            node.Code = refCode;
                        }
                    }
                    else
                    {
                        string notUsed = null;
                        node.Code += CacheRefScalarCs(out notUsed, scalarSimpleIdent, w.scalarCache, w.headerCs, EScalarRefType.OnRightHandSide, null, false, transformationAllowed, stringify);
                    }
                }
            }
            else
            {
                //not {s} or {%s}, but something like {%s1+%s2}
                node.Code += "O.ZScalar(" + node[0].Code + ")";
            }
        }

        //This method converts a simple scalar like '%s' into a reference to 'scalar117' (global IVariable), via the method O.GetScalarFromCache()
        private static string CacheRefScalarCs(out string scalarNameInGlobalCache, string scalarSimpleIdent, GekkoDictionary<string, string> scalarCache, StringBuilder headerCs, EScalarRefType type, string rhsCs, bool isName, bool transformationAllowed, bool stringify)
        {
            string scalarCs = null;
            scalarNameInGlobalCache = null; scalarCache.TryGetValue(scalarSimpleIdent, out scalarNameInGlobalCache);
            if (scalarNameInGlobalCache == null)
            {
                //this scalar name has not been encountered before
                scalarNameInGlobalCache = "scalar" + ++Globals.counter;
                scalarCache.Add(scalarSimpleIdent, scalarNameInGlobalCache);
                headerCs.AppendLine("public static IVariable " + scalarNameInGlobalCache + " = null;");                
            }            
            //using the scalar name (for instance scalar117) found or created in the global cache
            if (type == EScalarRefType.OnRightHandSide)
            {
                string b = "false";
                if (transformationAllowed) b = "true";
                string s = "false";
                if (stringify) s = "true";
                scalarCs = "O.GetScalarFromCache(ref " + scalarNameInGlobalCache + ", `" + scalarSimpleIdent + "`, " + b + ", " + s + ")";
            }
            else if (type == EScalarRefType.Val)
            {
                scalarCs = "O.SetValFromCache(ref " + scalarNameInGlobalCache + ", `" + scalarSimpleIdent + "`, " + rhsCs + ");" + G.NL;
            }
            else if (type == EScalarRefType.Date)
            {
                scalarCs = "O.SetDateFromCache(ref " + scalarNameInGlobalCache + ", `" + scalarSimpleIdent + "`, " + rhsCs + ");" + G.NL;
            }
            else if (type == EScalarRefType.String)
            {
                string x = "false";
                if (isName) x = "true";
                scalarCs = "O.SetStringFromCache(ref " + scalarNameInGlobalCache + ", `" + scalarSimpleIdent + "`, " + rhsCs + ", " + x + ");" + G.NL;
            }
            else if (type == EScalarRefType.Matrix)
            {
                //see also SetMatrixData(), should that be used??
                scalarCs = "O.SetMatrixFromCache(ref " + scalarNameInGlobalCache + ", `" + scalarSimpleIdent + "`, " + rhsCs + ");" + G.NL;
            }
            else throw new GekkoException();    
            return scalarCs;
        }        

        private static string FindFunctionArguments(ASTNode node, W wh2, string simpleIdent)
        {            
            if (wh2.functionHelper != null)
            {
                foreach (FunctionArgumentsHelperElements fah in wh2.functionHelper.storage)
                {
                    if (G.equal(fah.parameterName, simpleIdent))
                    {
                        //this list is a function argument, use that                        
                        return fah.parameterCode;
                    }
                }
            }
            return null;
        }        

        private static void GetCodeFromAllChildren(ASTNode node)
        {
            foreach (ASTNode child in node.ChildrenIterator())
            {
                node.Code += child.Code + G.NL;
            }
        }


        private static string AstBankHelperList(ASTNode node, W wh2)
        {
            string bankNumberCode = null;
            if (wh2.wh.currentCommand == "ASTPRT")
            {
                bankNumberCode = "bankNumber";
            }
            else
            {
                bankNumberCode = "1";  //default: Work
            }
            string code = "O.GetListWithBankPrefix(" + node[0].Code + ", " + node[1].Code + ", " + bankNumberCode + ")";            
            return code;
        }
        
        
        private static string AstBankHelper(ASTNode node, W wh2, int type)
        {
            string isLhsSoCanAutoCreate = null;
            if ((node.Number == 1 && (node.Parent.Text == "ASTGENR" || node.Parent.Text == "ASTGENRLHSFUNCTION")) || node.Parent.Text == "ASTTUPLEITEM")
            {
                isLhsSoCanAutoCreate = ", O.ECreatePossibilities.Can";
            }
            else if (node.Number == 0 && node.Parent.Text == "ASTCREATEEXPRESSION")
            {
                isLhsSoCanAutoCreate = ", O.ECreatePossibilities.Must";
            }
            
            string bankNumberCode = null;
            if (wh2.wh.currentCommand == "ASTPRT")
            {
                bankNumberCode = "bankNumber";
            }
            else
            {
                bankNumberCode = "1";  //default: Work
            }

            if (type == 1)
            {
                string listFallBackCode = null;
                if (node[0].ChildrenCount() == 0)
                {
                    listFallBackCode = node[1].Code;  //this is a ScalarString already, and we want to avoid a superfluous 'Work:a' for an 'a' item. So this will run ok fast for simple items.
                }
                else
                {
                    listFallBackCode = "new ScalarString(O.GetString(" + node[0].Code + ") + `:` + O.GetString(" + node[1].Code + "))";
                }
                return listFallBackCode;
            }
            else if (type == 2) //minus
            {
                string listFallBackCode = null;
                if (node[0][0].ChildrenCount() == 0)
                {
                    listFallBackCode = "O.Add(new ScalarString(`-`), " + node[0][1].Code + ", t)";  //this is a ScalarString already, and we want to avoid a superfluous 'Work:a' for an 'a' item. So this will run ok fast for simple items.
                }
                else
                {
                    listFallBackCode = "new ScalarString(O.GetString(" + node[0][0].Code + ") + `:-` + O.GetString(" + node[0][1].Code + "))";
                }
                return listFallBackCode;
            }

            string fallBackCode = "O.GetString(" + node[0].Code + ") + `:` + O.GetString(" + node[1].Code + ")";
            
            string simpleHash = null;
            //node[1].ChildrenCount() is always > 0
            if (node[0].ChildrenCount() > 0)
            {
                //there is a bank like a:b or %x:b or %x:%y
                if (node[0][0].nameSimpleIdent != null && node[1][0].nameSimpleIdent != null)
                {
                    //simple names like a:b
                    simpleHash = node[0][0].nameSimpleIdent + ":" + node[1][0].nameSimpleIdent;
                }
            }
            else
            {
                //name like a or %x
                if (node[1][0].nameSimpleIdent != null)
                {
                    //simple name like a
                    simpleHash = node[1][0].nameSimpleIdent;
                }
            }

            bool isSimple = false; if (simpleHash != null) isSimple = true;
                        
            string code = null;
            
            string fa;
            int choice;
            GetChoice(node, wh2, simpleHash, isSimple, out fa, out choice);

            if (choice == 1)
            {
                node.Code += fa;
            }
            else if (choice == 2)
            {
                //isSimple means that the name is simple like a or b:a.
                //Then we look for it in the global cache
                string s = null; wh2.tsCache.TryGetValue(simpleHash, out s);
                if (s == null)
                {
                    //has not been seen before
                    string ivWithNumber = "iv" + ++Globals.counter;
                    wh2.tsCache.Add(simpleHash, ivWithNumber);
                    wh2.headerCs.AppendLine("public static IVariable " + ivWithNumber + " = null;");  //cannot set it to ScalarVal since it may change type...                    
                    node.Code += "O.GetTimeSeriesFromCache(ref " + ivWithNumber + ", `" + simpleHash + "`, " + bankNumberCode + isLhsSoCanAutoCreate + ")";
                }
                else
                {
                    node.Code += "O.GetTimeSeriesFromCache(ref " + s + ", `" + simpleHash + "`, " + bankNumberCode + isLhsSoCanAutoCreate + ")";
                }
            }
            else if (choice == 3)
            {
                //This means that the name is complicated like %x or b:%x or %y:a or %x:%y (or fx%i)                
                if (wh2.wh.localStatementCache != null)
                {
                    //GENR statement for instance, maybe also VAL if indexer fY[2010]??
                    //This means there is a GENR statement at the top of the AST tree
                    //In that case, we look for the variable in the local cache
                    string fallBackCode2 = "O.GetTimeSeries(" + fallBackCode + ", " + bankNumberCode + isLhsSoCanAutoCreate + ")";  //here, bankNumberCode will always be = "1", since this is not a PRT statement
                    string xx = null; wh2.wh.localStatementCache.TryGetValue(fallBackCode2, out xx);
                    if (xx != null)
                    {
                        //This complicated timeseries (or scalar) has been seen before in this particular GENR/PRT statement                        
                        code = xx;
                    }
                    else
                    {
                        string refCode = "ts" + ++Globals.counter;
                        wh2.wh.localStatementCache.Add(fallBackCode2, refCode);
                        code = refCode;
                    }
                }
                else
                {
                    node.Code += "O.GetTimeSeries(" + fallBackCode + ", " + bankNumberCode + isLhsSoCanAutoCreate + ")";
                    //Complicated name, but not inside a GENR statement: just use the statement directly without use of any caches
                }
            }
            else throw new GekkoException();
            return code;
        }

        private static void GetChoice(ASTNode node, W wh2, string simpleHash, bool isSimple, out string fa, out int choice)
        {
            fa = null;
            choice = 3;  //1, 2, 3
            if (isSimple)
            {
                fa = FindFunctionArguments(node, wh2, simpleHash);
                if (fa != null) choice = 1;
                else choice = 2;
            }
            if (choice == 2 && Globals.useCache == false) choice = 3;
        }

        private static string HandleNegate(ASTNode node)
        {
            //This is for speedup purposes, to avoid a Negate() function on primitives taking up time.
            //So we get ScalarVal(-2) instead of Negate(ScalarVal(2)).
            string minus = "";
            if (node.Parent != null)
            {
                if (node.Parent.Text == "NEGATE" || node.Parent.Text == "ASTINTEGERNEGATIVE" || node.Parent.Text == "ASTDOUBLENEGATIVE")
                {
                    //For parent NEGATE this parent will be ignored
                    minus = "-";
                    node.Parent.IgnoreNegate = true;
                }
                else if (node.Parent.Text == "ASTNAMEWITHDOT")
                {
                    //For parent ASTNAMEWITHBANK nothing is ignored 
                    minus = "-";                    
                }
            }
            return minus;
        }

        private static string ExtractInnerString(string s)
        {
            string s2 = s;
            if (s.StartsWith("new ScalarString(`") && s.EndsWith("`)"))
            {
                s2 = s.Substring(18, s.Length - 18 - 2);
            }
            else if (s.StartsWith("(new ScalarString(`") && s.EndsWith("`))"))
            {
                s2 = s.Substring(19, s.Length - 19 - 3);
            }
            return s2;
        }

        //private static void SendStuffUpwardsInTree2(ASTNode node, string scalarWithNumber, string s, WalkHelper2 wh2)
        //{
        //    if (true)
        //    {
        //        wh2.headerCs += "public static IVariable " + scalarWithNumber + " = null;" + G.NL;
        //        wh2.headerMethodScalarCs += scalarWithNumber + " = null;" + G.NL;
        //    }
        //    else
        //    {
        //        ASTNode node2 = node.Parent;
        //        bool succes = false;
        //        while (true)
        //        {
        //            if (node2 == null) break;
        //            //TODO TODO TODO TODO
        //            //TODO TODO TODO TODO
        //            //TODO TODO TODO TODO do something intellingent regarding uploading
        //            //TODO TODO TODO TODO
        //            //TODO TODO TODO TODO
        //            if (node2.Text == "ASTGENR" || node2.Text == "ASTVAL" || node2.Text == "ASTSTRING" || node2.Text == "ASTDATE" || node2.Text == "ASTPRTELEMENT")
        //            {
        //                node2.CodeSentFromSubTree += "IVariable " + scalarWithNumber + " = " + s + ";" + G.NL;
        //                succes = true;
        //                break;
        //            }
        //            node2 = node2.Parent;
        //        }
        //        if (succes == false)
        //        {
        //            node.Code += s;
        //        }
        //        else
        //        {
        //            node.Code += scalarWithNumber;
        //        }
        //    }
        //    return;
        //}

        //private static void SendTimeSeriesUpwardsInTree(ASTNode node, string hash, string tsWithNumber, string refCode, bool isSimple, WalkHelper2 wh2)
        //{
        //    //This TimeSeries is not known, seen for the first time in the file.
        //    string[] split = hash.Split(',');
        //    string bank = split[0].Trim();
        //    string variable = split[1].Trim();
        //    string hash2 = "O.GetString(" + bank + "), O.GetString(" + variable + ")";
                        
        //    if (isSimple)
        //    {
        //        //name like adambk:fy, not bank%1:fx%i
        //        wh2.headerCs += "public static IVariable " + refCode + " = null;" + G.NL;
        //        //wh2.headerMethodTsCs += "((MetaTimeSeries)" +refCode + ").ts = null;" + G.NL;  //clear this
        //        wh2.headerMethodTsCs += "" + refCode + " = null;" + G.NL;  //clear this
        //    }
        //    else
        //    {
        //        //composed name like bank%1:fx%i
        //        ASTNode node2 = node.Parent;
        //        bool succes = false;
        //        while (true)
        //        {
        //            if (node2 == null) break;
        //            //TODO TODO TODO TODO
        //            //TODO TODO TODO TODO
        //            //TODO TODO TODO TODO do something intellingent regarding uploading
        //            //TODO TODO TODO TODO
        //            //TODO TODO TODO TODO                

        //            if (node2.Text == "ASTGENR" || node2.Text == "ASTVAL" || node2.Text == "ASTSTRING" || node2.Text == "ASTDATE" || node2.Text == "ASTUPD")
        //            {
        //                node2.CodeSentFromSubTree += "IVariable " + tsWithNumber + " = new MetaTimeSeries(O.GetTS(" + hash2 + "));" + G.NL;                        
        //                succes = true;
        //                break;
        //            }
        //            else if (node2.Text == "ASTPRTELEMENT")
        //            {
        //                //This variant reacts to a bankNumber (if for instance called with <m> or <b>).
        //                node2.CodeSentFromSubTree += "IVariable " + tsWithNumber + " = new MetaTimeSeries(O.GetTS(" + hash2 + ", bankNumber));" + G.NL;  //null, null because it is not a simple name
        //                succes = true;
        //                break;
        //            }
        //            node2 = node2.Parent;
        //        }
        //        if (succes == false)
        //        {
        //            //node.Code += "new MetaTimeSeries(O.GetTS(" + hash + "))" + G.NL;
        //            //G.Writeln2("*** ERROR: Unexpected use of time series: did you intend to use a scalar?");
        //            //throw new GekkoException();
        //            node.Code += "new MetaTimeSeries(O.GetTS(" + hash2 + "))";
        //        }
        //        else
        //        {
        //        }
        //    }
        //    return;
        //}        

        private static string AddPrintCode(string type, string s, string parentType, ASTNode node)
        {
            string o = "o" + Num(node) + ".printCodes";
            if (parentType == "ASTPRTELEMENTOPTIONFIELD") o = "ope" + Num(node) + ".printCodes";
            return o + ".Add(new OptString(`" + type + "`, O.GetString(" + s + ")));" + G.NL;
        }        

        //private static string CondenseChildrenNodesToList(ASTNode node)  //last arg. just so that it is more likely that the user remembers to augment Globals.counter before call
        //{
        //    Globals.counter++;
        //    string name = "l" + Globals.counter;
        //    node.GetCode().AppendLine("List<string>" + name + " = new List<string>();");
        //    foreach (ASTNode child in node.ChildrenIterator())
        //    {
        //        node.GetCode().AppendLine(name + ".Add(" + child.GetCode() + ");");
        //    }
        //    return name;
        //}

        //private static void CondenseChildrenNodesToString(ASTNode node, string divider)
        //{
        //    foreach (ASTNode child in node.ChildrenIterator())
        //    {
        //        node.GetCode().Append(child.GetCode().ToString());
        //        if (!child.IsLastChild()) node.GetCode().Append(" " + divider + " ");
        //    }
        //}

        private static void Emit1(ASTNode node, GekkoST st)
        {
            Globals.counter++;
            st.Append("List<string> xx{#} = new List<string>();" + G.NL);
            foreach (ASTNode child in node.ChildrenIterator())
            {
                st.Append("xx{#}.Add({0});", child);
            }
        }        

        private static void CreateOptionVariable(ASTNode node, StringBuilder s, ref string o)
        {
            StringBuilder s1 = new StringBuilder();
            StringBuilder s1a = new StringBuilder();
            StringBuilder s2 = new StringBuilder();
            string type = null;
            s2.Append("Program.options.");
            for (int i = 0; i < node.ChildrenCount(); ++i)
            {
                ASTNode child = node[i];

                if (child.Text == "?")
                {
                    s.Append("Program.PrintOptions(`" + s2.ToString() + s1.ToString() + "`);");
                    return;
                }

                if (i < node.ChildrenCount() - 2)  //up to and including third-last
                {
                    s1.Append(child.Text.ToLower() + "_");
                }
                else if (i == node.ChildrenCount() - 2)  //second-last
                {
                    s1.Append(child.Text.ToLower());
                }
                else if (i == node.ChildrenCount() - 1)  //last
                {
                    if (child.Text == "ASTINTEGER")
                    {
                        type = "val";
                        if (child.GetChild(0).Text == "-")
                        {
                            s1a.Append(child.GetChild(0).Text);
                            s1a.Append(child.GetChild(1).Text);
                        }
                        else
                        {
                            s1a.Append(child.GetChild(0).Text);
                        }
                    }
                    else if (child.Text == "ASTDOUBLE")
                    {
                        type = "val";
                        int index = 0;
                        if (child.GetChild(0).Text == "-")
                        {
                            s1a.Append(child.GetChild(0).Text);
                            index = 1;
                        }
                        string s5 = child.GetChild(index).Text;
                        s5 = Program.MaybeAddPointAndZero(s5);
                        s1a.Append(s5);
                    }
                    else if (child.Text == "ASTBOOL")
                    {
                        type = "string";
                        if (G.equal(child.GetChild(0).Text, "astyes") || G.equal(child.GetChild(0).Text, "true"))
                        {
                            s1a.Append("true");
                        }
                        else if (G.equal(child.GetChild(0).Text, "astno") || G.equal(child.GetChild(0).Text, "false"))
                        {
                            s1a.Append("false");
                        }
                        else
                        {
                            G.Writeln("*** ERROR with options");
                            throw new GekkoException();
                        }
                    }
                    else if (child.Text == "ASTSTRINGSIMPLE")
                    {
                        type = "string";
                        string temp = "";                        
                        string s7 = null;
                        if (child[0].Code == null)
                        {
                            s7 = "`" + child[0].Text + "`";
                        }
                        else
                        {
                            s7 = "O.GetString(" + child[0].Code + ")";
                        }
                        if (G.equal(node[0].Text, "freq"))  //OPTION freq = ...
                        {
                            s7 = "G.GetFreq(" + s7 + ")";
                        }
                        temp += s7;
                        //}
                        s1a.Append(temp);
                    }
                    else
                    {
                        throw new GekkoException();
                    }
                }
                else throw new GekkoException();
            }
            o = s1.ToString();
            s.Append(s2);
            s.Append(s1);
            s.Append(" = ");
            s.Append(s1a);
            s.AppendLine(";");            

            StringBuilder s3 = new StringBuilder();
            s3.Append("option_");
            s3.Append(s1);
            s3.Append(" = ");
            string s1b = s1a.ToString();
            if (s1b.StartsWith("@")) s1b = s1b.Substring(1);
            s3.Append(s1b);
            s3.Replace(@"\", @"\\");
            s3.Replace("_", " ");
            s3.Replace("`", "");
            s3.Replace("true", "yes");
            s3.Replace("false", "no");
            s.AppendLine("G.Writeln();");
            //#987350932752
            //s.AppendLine("G.Writeln(Program.SubstituteAssignVars(`" + s3.ToString() + "`));");

            string sss = s1a.ToString();
            s1 = s1.Replace("_", " ");
            sss = sss.Replace("True", "`yes`");
            sss = sss.Replace("False", "`no`");
            sss = sss.Replace("true", "`yes`");
            sss = sss.Replace("false", "`no`");
            //string ss = "IVariable iv = " + s1a + ";";
            //if (type == "string")
            //{
                s.AppendLine("G.Writeln(`option " + s1.ToString() + " = ` + " + sss + " + ``);");
            //}
            //else if (type == "val")
            //{
                //s.AppendLine("G.Writeln(" + s1.ToString() + "O.GetVal(`" + s3.ToString() + "`));");
            //    s.AppendLine("G.Writeln(" + s1.ToString() + "(`" + s1a.ToString() + "`));");
            //}
        }

        private static void CreateOptionVariableOLD(ASTNode node, StringBuilder s, ref string o)
        {
            StringBuilder s1 = new StringBuilder();
            StringBuilder s1a = new StringBuilder();
            StringBuilder s2 = new StringBuilder();
            s2.Append("Program.options.");
            for (int i = 0; i < node.ChildrenCount(); ++i)
            {
                ASTNode child = node[i];

                if (child.Text == "?")
                {
                    s.Append("Program.PrintOptions(`" + s2.ToString() + s1.ToString() + "`);");
                    return;
                }

                if (i < node.ChildrenCount() - 2)  //up to and including third-last
                {
                    s1.Append(child.Text.ToLower() + "_");
                }
                else if (i == node.ChildrenCount() - 2)  //second-last
                {
                    s1.Append(child.Text.ToLower());
                }
                else if (i == node.ChildrenCount() - 1)  //last
                {
                    if (child.Text == "ASTINTEGER")
                    {
                        if (child.GetChild(0).Text == "-")
                        {
                            s1a.Append(child.GetChild(0).Text);
                            s1a.Append(child.GetChild(1).Text);
                        }
                        else
                        {
                            s1a.Append(child.GetChild(0).Text);
                        }
                    }
                    else if (child.Text == "ASTDOUBLE")
                    {
                        int index = 0;
                        if (child.GetChild(0).Text == "-")
                        {
                            s1a.Append(child.GetChild(0).Text);
                            index = 1;
                        }
                        string s5 = child.GetChild(index).Text;
                        s5 = Program.MaybeAddPointAndZero(s5);
                        s1a.Append(s5);
                    }
                    else if (child.Text == "ASTBOOL")
                    {
                        if (G.equal(child.GetChild(0).Text, "yes") || G.equal(child.GetChild(0).Text, "true"))
                        {
                            s1a.Append("true");
                        }
                        else if (G.equal(child.GetChild(0).Text, "no") || G.equal(child.GetChild(0).Text, "false"))
                        {
                            s1a.Append("false");
                        }
                        else
                        {
                            G.Writeln("*** ERROR with options");
                            throw new GekkoException();
                        }
                    }
                    else if (child.Text == "ASTSTRINGSIMPLE")
                    {
                        string temp = "";
                        //if (child.GetChild(0).Text == "ASTFILENAME")  //very unlikely this should ever be used as a string name here
                        //{
                        //    ////When does this happen??????????????
                        //    //string s4 = "";
                        //    //ASTNode gchild = child.GetChild(0);
                        //    //foreach (ASTNode child2 in gchild.ChildrenIterator())
                        //    //{
                        //    //    s4 += child2.Text;
                        //    //}
                        //    //s4 = Program.StripQuotes(s4);
                        //    //temp += ("@`" + s4 + "`");  //@ because it can contain slashes

                        //    temp += ("O.GetString(" + child[0].Code + ")");  //@ because it can contain slashes
                        //}
                        //else
                        //{
                        string s7 = null;
                        if (child[0].Code == null)
                        {
                            s7 = "`" + child[0].Text + "`";
                        }
                        else
                        {
                            s7 = "O.GetString(" + child[0].Code + ")";                            
                        }
                        if (G.equal(node[0].Text, "freq"))  //OPTION freq = ...
                        {
                            s7 = "G.GetFreq(" + s7 + ")";
                        }
                        temp += s7;
                        //}
                        s1a.Append(temp);
                    }
                    else
                    {
                        throw new GekkoException();
                    }
                }
                else throw new GekkoException();
            }
            o = s1.ToString();            
            s.Append(s2);
            s.Append(s1);
            s.Append(" = ");
            s.Append(s1a);
            s.AppendLine(";");
            StringBuilder s3 = new StringBuilder();
            s3.Append("option_");
            s3.Append(s1);
            s3.Append(" = ");
            string s1b = s1a.ToString();
            if (s1b.StartsWith("@")) s1b = s1b.Substring(1);
            s3.Append(s1b);
            s3.Replace(@"\", @"\\");
            s3.Replace("_", " ");
            s3.Replace("`", "");
            s3.Replace("true", "yes");
            s3.Replace("false", "no");
            s.AppendLine("G.Writeln();");
            //#987350932752
            //s.AppendLine("G.Writeln(Program.SubstituteAssignVars(`" + s3.ToString() + "`));");
        }
    }

    public class W
    {       
        
        //Created when running a .cmd/.gek file
        public WalkHelper wh = null;
        //public ParserGekWalkASTAndEmit.ELastCommand lastCommand;
        public Dictionary<int, List<string>> prtItems;
        public Dictionary<int, List<string>> prtLabels;
        public string fileNameContainingParsedCode = null;
        public int commandLinesCounter = -1;
        public int expressionCounter = -1;
        public GekkoDictionary<string, string> scalarCache = new GekkoDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public GekkoDictionary<string, string> listCache = new GekkoDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public GekkoDictionary<string, string> tsCache = new GekkoDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public StringBuilder headerCs = new StringBuilder(); //stuff to be put at the very start.
        public StringBuilder headerMethodTsCs = new StringBuilder(); //stuff to clear TimeSeries pointers
        public StringBuilder headerMethodScalarCs = new StringBuilder(); //stuff to clear scalar pointers   
        public GekkoDictionary<string, bool> functionUserDefined = new GekkoDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        public GekkoDictionary<string, bool> tupleClasses = new GekkoDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        public FunctionArgumentsHelper functionHelper = null; //important that it starts out as null here
        //public Dictionary<string, EOptionType> opt = null;
    }

    public class FunctionArgumentsHelperElements
    {
        public string parameterName;
        public string type;
        public string parameterCode;
        public int tupleCount = 1;
        public string tupleNameCode;
    }

    public class FunctionArgumentsHelper
    {
        public List<string> lhsTypes = new List<string>();
        public string functionName;
        public List<FunctionArgumentsHelperElements> storage = new List<FunctionArgumentsHelperElements>();
    }

    public class WalkHelper
    {
        //created for each new command (except IF, FOR, etc -- hmm is this true now?)
        public GekkoDictionary<string, string> localStatementCache = null;
        //public StringBuilder localStatementCode = null;
        public string currentCommand = null;
        public bool isGotoOrTarget = false;
    }

    public class OPrt : O_OLD
    {
        private string fileName = null;
        private EDataFormat type = EDataFormat.None;  //type of data(bank)
        private bool merge = false;  //merge or not.
        private string as2 = null; //for OPEN AS.
        private string orientation = null;  //rows or cols

        public string FileName
        {
            get { return fileName; }
            set { FailIfImmutable(); fileName = value; }
        }

        public EDataFormat Type
        {
            get { return type; }
            set { FailIfImmutable(); type = value; }
        }

        public bool Merge
        {
            get { return merge; }
            set { FailIfImmutable(); merge = value; }
        }

        public string As
        {
            get { return as2; }
            set { FailIfImmutable(); as2 = value; }
        }

        public string Orientation
        {
            get { return orientation; }
            set { FailIfImmutable(); orientation = value; }
        }

    }
}
