﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gekko
{
    public class ScalarVal : IVariable
    {
        public double val;

        public ScalarVal(double d)
        {
            val = d;
        }

        public double GetVal(GekkoTime t)
        {
            return this.val;
        }

        public string GetString()
        {
            G.Writeln2("*** ERROR: Could not convert the VAL " + this.val + " directly into STRING.");
            G.Writeln("           You may try the string() conversion function.");            
            throw new GekkoException();
        }

        public GekkoTime GetDate(O.GetDateChoices c)
        {
            GekkoTime gt = Globals.tNull;
            int intValue = O.GetInt(this);  //will issue error if the VAL is not an integer
            int year = G.findYear(intValue);  //error is the year is crazy
            if (c == O.GetDateChoices.Strict || (c != O.GetDateChoices.Strict && (Program.options.freq == EFreq.Annual || Program.options.freq == EFreq.Undated)))
            {
                if (Program.options.freq == EFreq.Undated)
                {
                    gt = new GekkoTime(EFreq.Undated, year, 1); //here, the context matters!
                }
                else
                {
                    gt = new GekkoTime(EFreq.Annual, year, 1);  //for a, q, m
                    //so date d = 2000 in freq=m will not turn this into 2000m1 or 2000m12
                }
            }
            else
            {
                //that is, FlexibleStart or FlexibleEnd
                //For Annual and Undated, this has been handled above
                //typically for TIME 2000 2010 or SERIES<2000 2010> which are turned into
                //for instance 2000m1 to 2000m12.
                if (Program.options.freq == EFreq.Quarterly)
                {                    
                    int sub = 1;
                    if (c == O.GetDateChoices.FlexibleStart) sub = 1;
                    else if (c == O.GetDateChoices.FlexibleEnd) sub = 4;
                    gt = new GekkoTime(EFreq.Quarterly, year, sub);
                }
                else if (Program.options.freq == EFreq.Monthly)
                {                    
                    int sub = 1;
                    if (c == O.GetDateChoices.FlexibleStart) sub = 1;
                    else if (c == O.GetDateChoices.FlexibleEnd) sub = 12;
                    gt = new GekkoTime(EFreq.Monthly, year, sub);
                }
            }            
            return gt;
        }        

        public IVariable Indexer(IVariable index, GekkoTime t)
        {
            G.Writeln2("*** ERROR: You cannot use []-index on VAL");                
            throw new GekkoException();
        }


        public IVariable Indexer(IVariable index1, IVariable index2, GekkoTime t)
        {
            G.Writeln2("Val cannot used with [i, j] indexer");
            throw new GekkoException();
        }

        public IVariable Indexer(IVariablesFilterRange indexRange, GekkoTime t)
        {
            G.Writeln2("*** ERROR: You cannot use []-index on VAL");                
            throw new GekkoException();            
        }

        public IVariable Indexer(IVariablesFilterRange indexRange1, IVariablesFilterRange indexRange2, GekkoTime t)
        {
            throw new GekkoException();
        }

        public IVariable Indexer(IVariable index, IVariablesFilterRange indexRange, GekkoTime t)
        {
            throw new GekkoException();
        }

        public IVariable Indexer(IVariablesFilterRange indexRange, IVariable index, GekkoTime t)
        {
            throw new GekkoException();
        }        

        public List<string> GetList()
        {
            //See similar comment: #slkfhas
            G.Writeln2("*** ERROR: You are trying to convert/use the value " + this.val + " as a STRING/NAME item in a list");
            G.Writeln("           In LIST commands, you must for example use '5' instead of 5, and '01' instead of 01.");
            G.Writeln("           Alternatively, for lists with such elements, you can use the LIST<direct> option.");
            G.Writeln("           If you are using a VAL scalar %v, you may try to use string(%v) instead.");
            throw new GekkoException();
        }

        public EVariableType Type()
        {
            return EVariableType.Val;
        }
        
        public IVariable Negate(GekkoTime t)
        {
            return new ScalarVal(-this.val);
        }        

        public void InjectAdd(IVariable x, IVariable y, GekkoTime t)
        {
            if (x.Type() == EVariableType.Val)
            {
                if (y.Type() == EVariableType.Val)
                {
                    //Probably good to have this case first, to speed up the (VAL, VAL) combination

                    //We have to inject the value into the object. If we were to return a new object (or use the Add()
                    //method), the Dictionary holding the %-names would point to an old object with an old value.
                    //And this way, we also save 1 object creation which is good for performance.
                    this.val = ((ScalarVal)x).val + ((ScalarVal)y).val;
                }
                else if (y.Type() == EVariableType.Date)
                {
                }
                else if (y.Type() == EVariableType.String)
                {
                }
            }
            else if (x.Type() == EVariableType.Date)
            {
                G.Writeln("*** ERROR: Cannot convert from date to value");
                throw new GekkoException();
            }
            else if (x.Type() == EVariableType.String)
            {
                G.Writeln("*** ERROR: Cannot convert from string to value");
                throw new GekkoException();
            }
            else
            {
                G.Writeln("*** ERROR: Internal Gekko error #98734543");
                throw new GekkoException();
            }
        }

        public IVariable Add(IVariable x, GekkoTime t)
        {
            switch (x.Type())
            {
                case EVariableType.Val:
                    {
                        //Strangely, using a simple object pool for these objects does not do any speedup (maybe from 14.5s to 14.4s on one case).
                        //Apparently, small scalar objects like these are quickly created and swept. They probably stay in
                        //the first generation memory, and not much fragmentation occurs.
                        //All in all, it is pretty impressive from C#, and we avoid object pooling complexities and other horrors.                        
                        return new ScalarVal(this.val + ((ScalarVal)x).val);

                    }
                case EVariableType.TimeSeries:
                    {
                        return Operators.ValTimeSeries.Add(this, (MetaTimeSeries)x, t);
                    }
                case EVariableType.String:                
                    {
                        return Operators.StringVal.Add((ScalarString)x, this, true);
                    }
                case EVariableType.Matrix:
                    {
                        G.Writeln2("*** ERROR: You cannot add a MATRIX and a SCALAR.");
                        throw new GekkoException();
                    }
                default:
                    {
                        G.Writeln2("*** ERROR: Memory variable conversion error.");
                        throw new GekkoException();
                    }                    
            }            
        }

        public IVariable Subtract(IVariable x, GekkoTime t)
        {
            switch (x.Type())
            {
                case EVariableType.Val:
                    {                        
                        return new ScalarVal(this.val - ((ScalarVal)x).val);
                    }
                case EVariableType.TimeSeries:
                    {
                        return new ScalarVal(this.val - O.GetVal(x, t));
                    }                
                default:
                    {
                        G.Writeln2("*** ERROR: Memory variable conversion error.");
                        throw new GekkoException();
                    }
            }
        }

        public IVariable Multiply(IVariable x, GekkoTime t)
        {
            switch (x.Type())
            {
                case EVariableType.Val:
                    {
                        return new ScalarVal(this.val * ((ScalarVal)x).val);
                    }
                case EVariableType.TimeSeries:
                    {
                        return new ScalarVal(this.val * O.GetVal(x, t));
                    }
                case EVariableType.Matrix:
                    {
                        //This is allowed in AREMOS, too
                        double[,] a = O.GetMatrix(x).data;
                        double b = O.GetVal(this, t);
                        int m = a.GetLength(0);
                        int k = a.GetLength(1);
                        double[,] c = O.MultiplyMatrixScalar(a, b, m, k);
                        Matrix z = new Matrix();
                        z.data = c;
                        return z;
                    }                
                default:
                    {
                        G.Writeln2("*** ERROR: Memory variable conversion error.");
                        throw new GekkoException();
                    }
            }
        }

        public IVariable Divide(IVariable x, GekkoTime t)
        {
            switch (x.Type())
            {
                case EVariableType.Val:
                    {
                        return new ScalarVal(this.val / ((ScalarVal)x).val);
                    }
                case EVariableType.TimeSeries:
                    {
                        return new ScalarVal(this.val / O.GetVal(x, t));
                    }                
                default:
                    {
                        G.Writeln2("*** ERROR: Memory variable conversion error.");
                        throw new GekkoException();
                    }
            }
        }

        public IVariable Power(IVariable x, GekkoTime t)
        {
            switch (x.Type())
            {
                case EVariableType.Val:
                    {
                        return new ScalarVal(Math.Pow(this.val, ((ScalarVal)x).val));
                    }
                case EVariableType.TimeSeries:
                    {
                        return new ScalarVal(Math.Pow(this.val, O.GetVal(x, t)));
                    }                
                default:
                    {
                        G.Writeln2("*** ERROR: Memory variable conversion error.");
                        throw new GekkoException();
                    }
            }
        }
    }
}
