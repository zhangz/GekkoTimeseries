/*
    Gekko Timeseries Software (www.t-t.dk/gekko).
    Copyright (C) 2016, Thomas Thomsen, T-T Analyse.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program (see the file COPYING in the root folder).
    Else, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Configuration;
using System.Reflection;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Globalization;
using System.Collections;
using System.Xml;
using System.Runtime.InteropServices;
using SevenZip;
using System.Diagnostics;
using System.Configuration;
using System.Collections.ObjectModel;
using System.Timers;

namespace Gekko
{

    public delegate void SetTextDelegate(System.Windows.Forms.Control ctrl, string text);

    public partial class Gui : Form
    {

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
        
        public P p;

        public Image red = null;
        public Image yellow = null;
        public Image green = null;
        //public Image gray = null;
        public Image target = null;

        public RichTextBoxEx textBox1 = new RichTextBoxEx();
        public RichTextBoxEx textBoxTab2 = new RichTextBoxEx();
        public RichTextBoxEx textBoxTab3 = new RichTextBoxEx();

        // worker thread
        Thread threadWorkerThread;
        public string threadInput;
        // events used to stop worker thread
        ManualResetEvent threadEventStopThread;
        ManualResetEvent threadEventThreadStopped;
        // Delegate instances used to call user interface functions
        // from worker thread:
        public DelegateAddString threadDelegateAddString;
        public DelegateThreadFinished threadDelegateThreadFinished;
        public DelegateSetTitle threadDelegateSetTitle;
        // delegates used to call MainForm functions from worker thread
        public delegate void DelegateAddString(Object s);
        public delegate void DelegateThreadFinished();
        public delegate void DelegateSetTitle(Object s);

        //private TreeNode findNodeResult = null;
        private string typed = "";
        private bool wordMatched = false;
        //private Assembly assembly;
        //private Hashtable namespaces;
        //private TreeNode nameSpaceNode;
        private bool foundNode = false;
        private string currentPath;

        //static ComplexPopup cp1 = null;
        //static Popup cp2 = null;
        //Defined here as a static object, for ease of use
        public static Gui gui;

        public Gui()
        {
            this.listBoxAutoComplete = new GListBox();
            this.textBox1 = new RichTextBoxEx();
            //this.settings1 = new Settings();
            this.splitContainer1 = new SplitContainerFix();

            InitializeComponent();

            this.textBox2.Font = new System.Drawing.Font("Courier New", (float)((double)Program.options.interface_zoom / 100d) * 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

            //
            // textBox1
            //
            this.textBox1.AutoWordSelection = true;
            this.textBox1.BackColor = System.Drawing.SystemColors.Window;
            this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox1.ContextMenuStrip = this.contextMenuStrip1;
            this.textBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox1.Font = new System.Drawing.Font("Courier New", (float)((double)Program.options.interface_zoom / 100d) * 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox1.Location = new System.Drawing.Point(0, 0);
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.WordWrap = false;
            this.textBox1.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Both;
            this.textBox1.Size = new System.Drawing.Size(641, 276);
            this.textBox1.TabIndex = 0;
            this.textBox1.TabStop = false;
            this.textBox1.Text = "";
            this.textBox1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.richTextBox777_KeyDown);
            this.splitContainer1.Panel1.Controls.Add(this.textBox1);

            //
            // textBoxTab2
            //
            this.textBoxTab2.AutoWordSelection = true;
            this.textBoxTab2.BackColor = System.Drawing.SystemColors.Window;
            this.textBoxTab2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxTab2.ContextMenuStrip = this.contextMenuStrip1;
            this.textBoxTab2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxTab2.Font = new System.Drawing.Font("Courier New", (float)((double)Program.options.interface_zoom / 100d) * 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxTab2.Location = new System.Drawing.Point(0, 0);
            this.textBoxTab2.Name = "textBoxTab2";
            this.textBoxTab2.ReadOnly = true;
            this.textBoxTab2.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.textBoxTab2.Size = new System.Drawing.Size(641, 358);
            this.textBoxTab2.TabIndex = 0;
            this.textBoxTab2.TabStop = false;
            this.textBoxTab2.Text = "";
            this.tabPage2.Controls.Add(this.textBoxTab2);

            //
            // textBoxTab3
            //
            this.textBoxTab3.AutoWordSelection = true;
            this.textBoxTab3.BackColor = System.Drawing.SystemColors.Window;
            this.textBoxTab3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxTab3.ContextMenuStrip = this.contextMenuStrip1;
            this.textBoxTab3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxTab3.Font = new System.Drawing.Font("Courier New", (float)((double)Program.options.interface_zoom / 100d) * 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxTab3.Location = new System.Drawing.Point(0, 0);
            this.textBoxTab3.Name = "textBoxTab3";
            this.textBoxTab3.ReadOnly = true;
            this.textBoxTab3.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.textBoxTab3.Size = new System.Drawing.Size(641, 358);
            this.textBoxTab3.TabIndex = 0;
            this.textBoxTab3.TabStop = false;
            this.textBoxTab3.Text = "";
            this.tabPage3.Controls.Add(this.textBoxTab3);

            // initialize delegates
            threadDelegateAddString = new DelegateAddString(this.AddString);
            threadDelegateThreadFinished = new DelegateThreadFinished(this.ThreadFinished);
            threadDelegateSetTitle = new DelegateSetTitle(this.SetTitleEtc);
            // initialize events
            threadEventStopThread = new ManualResetEvent(false);
            threadEventThreadStopped = new ManualResetEvent(false);

            string version = Globals.gekkoVersion;
            //1.4.9 stuff
            version = G.PrintVersion(version, false);

            this.Text = "Gekko " + version;
            this.Name = "Gekko " + version;

            green = Image.FromFile(Application.StartupPath + "\\images\\green.png");
            yellow = Image.FromFile(Application.StartupPath + "\\images\\yellow.png");            
            red = Image.FromFile(Application.StartupPath + "\\images\\red.png");
            target = Image.FromFile(Application.StartupPath + "\\images\\target.png");

            this.textBox1.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this.textBox1_LinkClicked);
            this.textBoxTab2.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this.textBoxTab2_LinkClicked);
            this.textBoxTab3.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this.textBoxTab3_LinkClicked);

            //if (Globals.isBetaVersion) this.splitContainer1.Panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));

            //this.KeyDown += new KeyEventHandler(Gekko_KeyDown);
        }

        static void CrashHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            MessageBox.Show("Unexpected Gekko crash: " + e.Message + G.NL + "Terminating: " + args.IsTerminating);
        }

        ///
        [STAThread]
        public static void Main(string[] args)
        {
            //Code to handle unexpected crashes, for instance after hibernation
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(CrashHandler);
                        
            //args can be tested in VS, see Gekko project options, debug.
            string noini = null;
            string folder = null;
            string nogui = null;
            try
            {
                if (args != null && args.Length > 0)
                {
                    StringBuilder sb2 = new StringBuilder();
                    for (int i = 0; i < args.Length; i++) // Loop through array
                    {
                        string argument = args[i];
                        string trim = argument.Trim();
                        if (trim.ToLower() == "-noini")
                        {
                            noini = "true";
                            continue;
                        }
                        if (trim.ToLower() == "-nogui")
                        {
                            nogui = "true";
                            continue;
                        }
                        if (trim.ToLower().StartsWith("-folder:"))
                        {
                            folder = trim.Substring(8).Trim();
                            continue;
                        }
                        sb2.Append(trim + " ");
                    }
                    string sb3 = sb2.ToString();
                    if (sb3.Length > 0) Globals.gekkoExeParameters = sb3;
                }
            }
            catch { };

            Application.EnableVisualStyles();

            if (Globals.testVersion == true)
            {
                GuiStuff(folder, noini, nogui);
            }
            else
            {

                try
                {
                    GuiStuff(folder, noini, nogui);
                }
                catch (Exception e2)
                {
                    //This exception is only if Gekko can not even start up...
                    string stackTrace = Program.GetStackTraceWithOffset(e2);
                    string s = "";
                    s += "*** ERROR: Gekko encountered a problem.\n";
                    s += "\n";
                    s += "============ System output start =====================\n";
                    s += "           " + e2.ToString() + "\n";
                    //s += stackTrace + "\n";
                    //s += "           " + e2.Message + "\n";
                    s += "============ System output end =======================\n";
                    s += "\n";
                    //MessageBox.Show(s);
                    WindowMessageBox w = new WindowMessageBox();
                    w.textBox1.Text = s;
                    w.ShowDialog();
                }
            }
        }


        private void Rekur(int d)
        {
            Console.WriteLine(d);
            d++;
            if (d == 4)
            {
                throw new GekkoException();
            }
            if (d == 6) return;
            Rekur(d);
        }

        private void GuiAutoExecStuff()
        {
            if (Directory.Exists(Globals.ttPath2 + @"\GekkoCS"))
            {
                Globals.runningOnTTComputer = true;  //for some debugging                
                G.WritelnGray("DEBUGGING: Seems to be running on TT computer -- some debugging is switched on");
            }

            //if (true && Globals.runningOnTTComputer)
            //{
            //    Translators.Translate1();
            //    return;
            //}

            if (false)
            {
                //Testing nested exeptions
                try
                {
                    Rekur(0);
                }
                catch (Exception e)
                {
                    {
                        DialogResult myDialogResult = MessageBox.Show(Globals.guiDialogErrorText, Globals.guiDialogErrorCaption, MessageBoxButtons.YesNo);
                        if (myDialogResult == DialogResult.Yes) throw;
                    }
                }

                try
                {
                    Rekur(0);
                }
                catch (Exception e)
                {
                    DialogResult myDialogResult = MessageBox.Show("question", "caption", MessageBoxButtons.YesNo);
                    if (myDialogResult == DialogResult.Yes)
                    {
                        //yes was clicked
                        throw;
                    }
                }

            }

            this.StartThread(" ", true);  //to get a worker thread started
            CrossThreadStuff.SetTab("main", false);
            G.WriteDirs("small", false);

            if (Globals.isGammaVersion)
            {
                G.Writeln();
                G.Writeln("+-------------------------------------------+", Color.Red);
                G.Writeln("|   This is a GAMMA VERSION intended for    |", Color.Red);
                G.Writeln("|      imminent release as Gekko 2.0.       |", Color.Red);
                G.Writeln("+-------------------------------------------+", Color.Red);
                G.Writeln("                                             ", Color.Red);
                G.Writeln("NOTE: This gamma version has some new code", Color.Red);
                G.Writeln("      to handle long non-looping command files.", Color.Red);
                G.Writeln("      If this poses problems, try setting", Color.Red);
                G.Writeln("      OPTION system code split = 0.", Color.Red);
                G.Writeln();
                G.Writeln("NOTE: See the TRANSLATE command regarding", Color.Red);
                G.Writeln("      files in Gekko 1.8 or AREMOS format.", Color.Red);
                G.Writeln();
                //Program.Pause("This is an untested BETA VERSION" + G.NL + "Please do not use it for serious purposes");
                if (Globals.runningOnTTComputer)
                {
                    G.Writeln("GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA ", Color.Red);
                    G.Writeln("GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA ", Color.Red);
                    G.Writeln("GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA ", Color.Red);
                    G.Writeln("GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA ", Color.Red);
                    G.Writeln("GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA ", Color.Red);
                    G.Writeln("GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA GAMMA ", Color.Red);
                    G.Writeln();
                }
                else
                {
                    Program.Pause("GAMMA VERSION -- bugs can still appear: no guarantees");
                }
            }

            if (Globals.isBetaVersion)
            {
                G.Writeln();
                G.Writeln("+-------------------------------------------+", Color.Red);
                G.Writeln("|  This is a not fully tested BETA VERSION  |", Color.Red);
                G.Writeln("| Please do not use it for serious purposes |", Color.Red);
                G.Writeln("+-------------------------------------------+", Color.Red);
                G.Writeln();
                //Program.Pause("This is an untested BETA VERSION" + G.NL + "Please do not use it for serious purposes");
                if (Globals.runningOnTTComputer)
                {
                    G.Writeln("BETA BETA BETA BETA BETA BETA BETA BETA BETA BETA BETA BETA BETA BETA BETA ", Color.Red);
                    G.Writeln("BETA BETA BETA BETA BETA BETA BETA BETA BETA BETA BETA BETA BETA BETA BETA ", Color.Red);
                    G.Writeln("BETA BETA BETA BETA BETA BETA BETA BETA BETA BETA BETA BETA BETA BETA BETA ", Color.Red);
                    G.Writeln("BETA BETA BETA BETA BETA BETA BETA BETA BETA BETA BETA BETA BETA BETA BETA ", Color.Red);
                    G.Writeln("BETA BETA BETA BETA BETA BETA BETA BETA BETA BETA BETA BETA BETA BETA BETA ", Color.Red);
                    G.Writeln();
                }
                else
                {
                    Program.Pause("BETA VERSION -- no guarantees");
                }
            }

            if (Globals.isAlphaVersion)
            {
                G.Writeln();
                G.Writeln("+-------------------------------------------+", Color.Red);
                G.Writeln("|  This is a not fully tested ALPHA VERSION |", Color.Red);
                G.Writeln("| Please do not use it for serious purposes |", Color.Red);
                G.Writeln("+-------------------------------------------+", Color.Red);
                G.Writeln();
                //Program.Pause("This is an untested BETA VERSION" + G.NL + "Please do not use it for serious purposes");
                if (Globals.runningOnTTComputer)
                {
                    G.Writeln("ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ", Color.Red);
                    G.Writeln("ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ", Color.Red);
                    G.Writeln("ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ", Color.Red);
                    G.Writeln("ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ", Color.Red);
                    G.Writeln("ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ", Color.Red);
                    G.Writeln("ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ", Color.Red);
                    G.Writeln("ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ALPHA ", Color.Red);
                    G.Writeln();
                }
                else
                {
                    Program.Pause("ALPHA VERSION -- no guarantees");
                }
            }

            if (Globals.isPreviewVersion)
            {
                G.Writeln();
                G.Writeln("+-------------------------------------------+", Color.Red);
                G.Writeln("|     This is a BETA of Gekko 2.0           |", Color.Red);
                G.Writeln("| Please do NOT use it for serious purposes.|", Color.Red);
                G.Writeln("|  The input window is blue to remind you   |", Color.Red);
                G.Writeln("|  that this is not an ordinary version.    |", Color.Red);
                G.Writeln("+-------------------------------------------+", Color.Red);
                G.Writeln();
                //Program.Pause("This is an untested BETA VERSION" + G.NL + "Please do not use it for serious purposes");                
            }

            Program.CreateLocalCopyHelpChm();
            CrossThreadStuff.Zoom();

            //gekko.exe parameters are read first, and then afterwards any gekko.ini local file
            StartupExeAndIniStuff();
            CrossThreadStuff.Mode();
        }

        private void StartupExeAndIniStuff()
        {
            if (Globals.gekkoExeParameters != null)
            {
                //works as if it was an inputted command line from the GUI                
                this.StartThread(Globals.gekkoExeParameters, true);
            }
            if (Globals.noini == false) RunGekkoIniFile();
        }

        private void RunGekkoTabToTextStuff(string folder)
        {
            Globals.RunGekkoTabToTextStuff_folder = folder;
            this.StartThread("RunGekkoTabToTextStuff", true);  //well, should be done more nicely, maybe serializing arguments, or putting them into
        }

        private void RunGekkoIniFile()
        {
            this.StartThread(Globals.iniFileSecretName, true); //This "command" gets handled in handleObeyFiles. Better 'run' than 'add', since RUN looks in cmd/cmd1/cmd2 folders also. Run2 ignores if the file is not found
        }

        private static void GuiStuff(string folder, string noini, string nogui)
        {

            bool track = false;
            if (File.Exists(@"c:\test\testing1234.txt"))
            {
                //This is for debugging purposes on a user computer where Gekko has problems starting up.
                //In that case, creating such a file on the computer will show the message boxes.
                track = true;
            }

            if (track) MessageBox.Show("1");
            Globals.guiRecentFoldersCache.Capacity = 20;
            if (track) MessageBox.Show("2");
            try
            {
                //no need to fail here if the user.config file has become corrupt (it will not fail if the file is just missing)
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
                if (track) MessageBox.Show("3");
                Globals.userSettingsPath = config.FilePath;
            }
            catch
            {
                if (track) MessageBox.Show("3");
                Globals.userSettingsPath = "[unknown]";  //only used in Help --> About... menu.
            }            
            if (track) MessageBox.Show("4");
            DateTime t0 = DateTime.Now;

            double s = (DateTime.Now - t0).TotalMilliseconds;

            // Determine whether the directory exists, else create it (used for temporary files)
            if (track) MessageBox.Show("5");
            Program.CreateTempFilesFolder();
            if (track) MessageBox.Show("6");
            GetVersionFromAssembly();  //goes into Globals.gekkoVersion
            if (track) MessageBox.Show("7");
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            if (track) MessageBox.Show("8");
            gui = new Gui();
            if (track) MessageBox.Show("9");

            gui.textBox1.AppendText(Globals.blankUsedAsPadding); //to simulate a previous carriage return with 1 character indent
            gui.textBoxTab2.Text = "";
            gui.textBoxTab2.AppendText(" This window is used to show output from the main window, when \n");
            gui.textBoxTab2.AppendText(" this output is large in volume, or the details may not be of \n");
            gui.textBoxTab2.AppendText(" crucial interest to the user. When relevant, this kind of extra \n");
            gui.textBoxTab2.AppendText(" output will be accessible via a clickable link in the main window. \n");
            gui.textBoxTab2.AppendText(" \n");
            gui.textBoxTab2.AppendText(Globals.blankUsedAsPadding); //to simulate a previous carriage return with 1 character indent

            gui.textBoxTab3.Text = "";
            gui.textBoxTab3.AppendText(" This window is used to provide easy access to the help system, \n");
            gui.textBoxTab3.AppendText(" specifically help concerning specific commands and their syntax. \n");
            gui.textBoxTab3.AppendText(" You may write 'help' in the main window to activate the help \n");
            gui.textBoxTab3.AppendText(" system here.");
            gui.textBoxTab3.AppendText(" \n");
            gui.textBoxTab3.AppendText(Globals.blankUsedAsPadding); //to simulate a previous carriage return with 1 character indent
            if (track) MessageBox.Show("10");

            bool cleanup = false;
            bool makeNewFile = false;
            string file = System.Windows.Forms.Application.LocalUserAppDataPath + "\\GekkoTimeStamp.txt";
            try
            {
                if (File.Exists(file))
                {
                    DirectoryInfo di = new DirectoryInfo(file);
                    DateTime dt = di.LastWriteTime;
                    double days = (DateTime.Now - dt).TotalDays;
                    if (days < 0 || days > 2d)  //we get cleanup if invalid time, or > 2 days gone (or stamp file is missing)
                    {
                        cleanup = true;
                        makeNewFile = true;
                    }
                }
                else
                {
                    cleanup = true;
                    makeNewFile = true;
                }
                if (makeNewFile)
                {
                    StreamWriter sw = new StreamWriter(file);
                    sw.WriteLine("This file is a non-critical system file that informs Gekko about last usage time/date.");
                    sw.Flush();
                    sw.Close();
                }
            }
            catch
            {
                //do nothing
            }

            if (cleanup)  //This is only run if last Gekko start is more than 48 hours ago. To avoid superflous cleanup,
            {             //that may take up to 0.1 sec if there are many .mdl files or other stuff to look through.
                          //So we only get this short startup delay every 48 hours.
                DateTime t1 = DateTime.Now;
                try
                {
                    DeleteTemporaryGraphFilesFromLastSession();
                }
                catch (Exception e)
                {
                    //do nothing
                }

                try
                {
                    DeleteTemporaryFilesFromLastSession();  //also limits .mdl files here to 500 MB max = ca. 60 models.
                }
                catch (Exception e)
                {
                    //do nothing
                }
                G.Writeln();
                G.Writeln("General cleanup of temporary Gekko files (" + G.Seconds(t1) + ") -- done occasionally");
            }


            if (track) MessageBox.Show("11");
            UserSettings us = null;
            try
            {
                us = LoadUserSettings();
            }
            catch (Exception e)
            {
                us = new UserSettings();
            }
            if (track) MessageBox.Show("12");


            if (folder != null)
            {
                Program.options.folder_working = folder;
            }
            else Program.options.folder_working = G.GetWorkingFolder();  //if called from cmd prompt, it will be that folder -> may be overwritten later on

            if (track) MessageBox.Show("13");
            string s1 = G.GetWorkingFolder();
            if (track) MessageBox.Show("14");
            string s2 = G.GetProgramDir();
            if (track) MessageBox.Show("15");            
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (track) MessageBox.Show("15.1");
            if (folder == null)
            {
                if (G.equal(s1, s2))  //It happens that the we get c:\\... in one of them, and C:\\ in the other, so we use case-insensitive compare
                {
                    try
                    {
                        if (Globals.userSettings.WorkingFolder != "")
                        {
                            if (Directory.Exists(Globals.userSettings.WorkingFolder))
                            {
                                Program.options.folder_working = Globals.userSettings.WorkingFolder;
                            }
                        }
                    }
                    catch (Exception e) {
                        //May fail if xml file is corrupted
                        //var s10 = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoaming);
                        string s11 = e.InnerException.Message;
                        MessageBox.Show("Gekko: The user settings file seems corrupted. Working folder set to desktop folder: " + desktop + "\n\nYou may consider deleting the user settings file, cf. this message:\n" + s11);
                        Program.options.folder_working = desktop;
                    }
                }
            }
            if (track) MessageBox.Show("16");
            for (int i = 0; i < 2; i++)
            {
                if (track) MessageBox.Show("16.1");
                bool exc = false;                
                if (track) MessageBox.Show("16.3");
                if (!Directory.Exists(Program.options.folder_working))
                {
                    MessageBox.Show("Gekko: The working folder '" + Program.options.folder_working + "' does not seem to exist \n -- changed to desktop folder: " + desktop);
                    Program.options.folder_working = desktop;
                }

                //Testing write access of working folder (writing a file, and deleting it again)
                try
                {
                    //Do not use 'using' and G.GekkoStreamWriter() here -- it is just a quick test, and will be caught if it fails!
                    Globals.screenOutput = new StreamWriter(Program.options.folder_working + "\\" + "delete_ksajrhdfjdssdj.txt");
                }
                catch (Exception e)
                {
                    exc = true;
                    MessageBox.Show("Gekko: The working folder '" + Program.options.folder_working + "' does not seem to have write access \n -- changed to desktop folder: " + desktop);
                    //MessageBox.Show(e.Message);
                    Program.options.folder_working = desktop;
                }
                if (track) MessageBox.Show("16.4");
                if (Globals.screenOutput != null)
                {
                    Globals.screenOutput.Flush();
                    Globals.screenOutput.Close();
                }

                if (track) MessageBox.Show("16.5");
                if (File.Exists(Program.options.folder_working + "\\" + "delete_ksajrhdfjdssdj.txt"))
                {
                    if (track) MessageBox.Show("16.6");
                    try
                    {
                        File.Delete(Program.options.folder_working + "\\" + "delete_ksajrhdfjdssdj.txt");  //best not to use WaitForFileDelete() here, since it is ok if delete fails here
                    }
                    catch { };
                }
                if (track) MessageBox.Show("16.7");
                if (exc == false) break;
            }
            if (track) MessageBox.Show("17");
            Databank work = new Databank(Globals.Work);
            if (track) MessageBox.Show("18");
            Databank base2 = new Databank(Globals.Ref);
            if (track) MessageBox.Show("20");
            Program.databanks.storage.Clear();  //just in case
            Program.databanks.storage.Add(work);
            Program.databanks.storage.Add(base2);            
            if (track) MessageBox.Show("21");

            Program.GetStartingPeriod();
            
            Globals.globalPeriodTimeSpans.data.Clear();  //just for safety
            Globals.globalPeriodTimeSpans.data.Add(new GekkoTimeSpan(Globals.globalPeriodStart, Globals.globalPeriodEnd));  //starts out with same TimeSpan as Time period

            if (track) MessageBox.Show("22");
            Program.ShowPeriodInStatusField("");
            if (track) MessageBox.Show("23");
            G.SetWorkingFolder();
            if (track) MessageBox.Show("24");
            G.Writeln();

            if (track) MessageBox.Show("25");
            Globals.guiRecentFoldersCache[Program.options.folder_working] = Program.options.folder_working;
            if (track) MessageBox.Show("26");
            GuiUpdateRecentFilesMenu();
            if (track) MessageBox.Show("27");
            //string helpfile = "I_OVERVIEW";
            //try
            //{
            //    Program.Help(helpfile);  //can contains errors is user fiddles with it, therefore try{}
            //}
            //catch
            //{
            //    MessageBox.Show("Failed to auto-load help file " + helpfile + ".rtf" + "\nIt may be missing or in a wrong format");
            //}

            if (noini != null && noini == "true")
            {
                Globals.noini = true;  //has to be put in here, to be fetched later on by GuiAutoExecStuff
            }

            //Running in silent mode is probably done here.
            //We probably need to call this.StartThread(" ", true), but problem is that 'this' does not exist.            

            try
            {
                Application.Run(gui);
            }
            catch
            {
                throw;
            }
            finally  //makes sure usersettings are always saved no matter what (for instance chosen working folder etc.)
            {                
                Globals.applicationIsInProcessOfDying = true;
                //#3452345523
                //Maybe just here, and not in EXIT and altF4.
                //Any errors etc should be in msgBox, so have a msgbox param.                
                //Write any pending OPEN databanks:
                int w = -12345;
                int b = -12345;
                Program.MaybeWriteOpenDatabanks(ref w, ref b);  //w and b are not used
                
                if (track) MessageBox.Show("28");
                if (Globals.pipeFileHelper.pipeFile != null)
                {
                    Globals.pipeFileHelper.CloseFile();
                }
                if (Globals.pipeFileHelper2.pipeFile != null)
                {
                    Globals.pipeFileHelper2.CloseFile();
                }

                if (track) MessageBox.Show("29");
                if (Globals.doNotSaveUserSettings == false)
                {
                    SaveUserSettings(us);
                }
                if (track) MessageBox.Show("30");

                //see also id7372367
                if (Globals.objApp != null)
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(Globals.objApp);
                    Globals.objApp = null;
                }
                GC.Collect();
                GC.WaitForPendingFinalizers();
                // GC needs to be called twice in order to get the Finalizers called
                // - the first time in, it simply makes a list of what is to be
                // finalized, the second time in, it actually is finalizing. Only
                // then will the object do its automatic ReleaseComObject.
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }        

        private static void GetVersionFromAssembly()
        {
            try
            {
                string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                if (version.EndsWith(".0"))
                {
                    version = version.Substring(0, version.Length - 2);
                }
                Globals.gekkoVersion = version;
            }
            catch (Exception e) { };
        }


        /// <summary>
        /// Files of type temp*.emf/dat/gp in \gnuplot\tempfiles are deleted
        /// each time the program starts up.
        /// Cannot be deleted after each graph, because of file lock issues.
        /// </summary>
        private static void DeleteTemporaryGraphFilesFromLastSession()
        {
            try
            {
                //delete temporary graph files
                //String path = Application.StartupPath + "\\gnuplot\\tempfiles";
                String path = System.Windows.Forms.Application.LocalUserAppDataPath + "\\gnuplot\\tempfiles";
                if (!Directory.Exists(path)) return;

                string[] imgList = Directory.GetFiles(path, "temp*.emf");
                foreach (string img in imgList)
                {
                    FileInfo imgInfo = new FileInfo(img);
                    imgInfo.Delete();
                }
                imgList = Directory.GetFiles(path, "temp*.dat");
                foreach (string img in imgList)
                {
                    FileInfo imgInfo = new FileInfo(img);
                    imgInfo.Delete();
                }
                imgList = Directory.GetFiles(path, "temp*.gp");
                foreach (string img in imgList)
                {
                    FileInfo imgInfo = new FileInfo(img);
                    imgInfo.Delete();
                }
            }
            catch (Exception e)
            {
                //fail silently, sometimes the files may be locked temporarily
            }

        }

        private static void DeleteTemporaryFilesFromLastSession()
        {
            string path = System.Windows.Forms.Application.LocalUserAppDataPath + "\\tempfiles";
            try
            {
                if (!Directory.Exists(path)) return;
                Program.DeleteFolder(path, "mdl");
            }
            catch (Exception e)
            {
                //fail silently, sometimes the files may be locked temporarily
            }

            try
            {
                foreach (DirectoryInfo dir in new DirectoryInfo(path).GetDirectories())
                {
                    dir.Delete();  //deletes only if it is empty
                }
            }
            catch
            {
                //fail silently
            }

            try
            {
                long sumMax = 500L * 1000000L;  //500 MB, if over it is pruned down to 80% of this (400 MB), deleting 100 MB of oldest models
                                                //500 MB corresponds to > 60 different large models
                double fraction = 0.8d;
                long sum = 0L;

                FileInfo[] files = new DirectoryInfo(path).GetFiles();

                List<DateTimeHelper> ddd = new List<DateTimeHelper>();
                foreach (FileInfo file in files)
                {
                    if (G.equal(file.Extension, ".mdl"))
                    {
                        sum += file.Length;
                        ddd.Add(new DateTimeHelper() { dt = file.LastWriteTime, s = file.FullName, size = file.Length });
                    }
                }

                if (sum > sumMax)
                {
                    //StringBuilder sb = new StringBuilder();
                    //foreach (DateTimeHelper d in ddd) Console.WriteLine(d.dt + " " + d.size + " " + d.s);
                    ddd.Sort((a, b) => a.dt.CompareTo(b.dt));
                    //Console.WriteLine();
                    //foreach (DateTimeHelper d in ddd) Console.WriteLine(d.dt + " " + d.size + " " + d.s);
                    double temp = (double)sumMax * fraction;  //we crop it some more, so we don't have to delete files for a while (= speedy startup)
                    long tooMuch = sum - (long)(temp);
                    long sum2 = 0L;
                    foreach (DateTimeHelper d in ddd)
                    {
                        sum2 += d.size;
                        //try
                        {
                            File.Delete(d.s);  //best not to use WaitForFileDelete() here, since failure is caught when calling the method (cleanup)
                        }
                        //catch
                        {
                            //do nothing
                        }
                        if (sum2 > tooMuch) break;
                    }
                }
            }
            catch (Exception e)
            {
                //fail silently, sometimes the files may be locked temporarily
            }

        }

        private static UserSettings LoadUserSettings()
        {
            //load user settings from xml file (see UserSettings.cs)
            UserSettings us = Globals.userSettings;
            if (us.UpgradeRequired)
            {

                us.Upgrade();
                us.Reload();  //necessay? but does not hurt
                us.UpgradeRequired = false;
                us.Save();
            }

            //window stuff start---------------------------
            int mainWindowWidthOLD = gui.Size.Width;
            int mainWindowHeightOLD = gui.Size.Height;
            int mainWindowTopDistanceOLD = gui.Top;
            int mainWindowLeftDistanceOLD = gui.Left;
            int mainWindowSplitterDistanceOLD = gui.splitContainer1.SplitterDistance;

            try
            {
                gui.Size = new Size(us.MainWindowWidth, us.MainWindowHeight);
                gui.Location = new Point(us.MainWindowLeftDistance, us.MainWindowTopDistance);
                //this one can go wrong

                if (false)
                {
                    //Does not work. Splitter drops to bottom when closing/opening several times,
                    //even when splitter is not touched.
                    gui.splitContainer1.SplitterDistance = us.MainWindowSplitterDistance;
                }
                else
                {
                    //need to fix splitter to something fixed!
                    //height is 358 always, but it gets ok even if we change the height of Gekko
                    gui.splitContainer1.SplitterDistance = (gui.splitContainer1.Height * 65) / 100;
                }
            }
            catch
            {
                //setting the defaults instead, they should work
                gui.Size = new Size(mainWindowWidthOLD, mainWindowHeightOLD);
                gui.Location = new Point(mainWindowLeftDistanceOLD, mainWindowTopDistanceOLD);
                gui.splitContainer1.SplitterDistance = mainWindowSplitterDistanceOLD;
            }
            //window stuff end---------------------------


            if (gui.Size.Height < 20 || gui.Size.Width < 20 || gui.Location.X < 0 || gui.Location.Y < 0)
            {
                gui.Size = new Size(700, 550);
                gui.Location = new Point(100, 50);
                gui.splitContainer1.SplitterDistance = 276;
            }

            try
            {
                if (us.RecentFolders20 != "") Globals.guiRecentFoldersCache[us.RecentFolders20] = us.RecentFolders20;
                if (us.RecentFolders19 != "") Globals.guiRecentFoldersCache[us.RecentFolders19] = us.RecentFolders19;
                if (us.RecentFolders18 != "") Globals.guiRecentFoldersCache[us.RecentFolders18] = us.RecentFolders18;
                if (us.RecentFolders17 != "") Globals.guiRecentFoldersCache[us.RecentFolders17] = us.RecentFolders17;
                if (us.RecentFolders16 != "") Globals.guiRecentFoldersCache[us.RecentFolders16] = us.RecentFolders16;
                if (us.RecentFolders15 != "") Globals.guiRecentFoldersCache[us.RecentFolders15] = us.RecentFolders15;
                if (us.RecentFolders14 != "") Globals.guiRecentFoldersCache[us.RecentFolders14] = us.RecentFolders14;
                if (us.RecentFolders13 != "") Globals.guiRecentFoldersCache[us.RecentFolders13] = us.RecentFolders13;
                if (us.RecentFolders12 != "") Globals.guiRecentFoldersCache[us.RecentFolders12] = us.RecentFolders12;
                if (us.RecentFolders11 != "") Globals.guiRecentFoldersCache[us.RecentFolders11] = us.RecentFolders11;

                if (us.RecentFolders10 != "") Globals.guiRecentFoldersCache[us.RecentFolders10] = us.RecentFolders10;
                if (us.RecentFolders9 != "") Globals.guiRecentFoldersCache[us.RecentFolders9] = us.RecentFolders9;
                if (us.RecentFolders8 != "") Globals.guiRecentFoldersCache[us.RecentFolders8] = us.RecentFolders8;
                if (us.RecentFolders7 != "") Globals.guiRecentFoldersCache[us.RecentFolders7] = us.RecentFolders7;
                if (us.RecentFolders6 != "") Globals.guiRecentFoldersCache[us.RecentFolders6] = us.RecentFolders6;
                if (us.RecentFolders5 != "") Globals.guiRecentFoldersCache[us.RecentFolders5] = us.RecentFolders5;
                if (us.RecentFolders4 != "") Globals.guiRecentFoldersCache[us.RecentFolders4] = us.RecentFolders4;
                if (us.RecentFolders3 != "") Globals.guiRecentFoldersCache[us.RecentFolders3] = us.RecentFolders3;
                if (us.RecentFolders2 != "") Globals.guiRecentFoldersCache[us.RecentFolders2] = us.RecentFolders2;
                if (us.RecentFolders1 != "") Globals.guiRecentFoldersCache[us.RecentFolders1] = us.RecentFolders1;
            }
            catch (Exception e)
            {
                //clearing this, may be corrupted?
                Globals.guiRecentFoldersCache = new Program.Cache(typeof(string));
            }

            try
            {
                //Globals.guiRecentFoldersCache.
                GuiUpdateRecentFilesMenu();
            }
            catch
            {
                //ok if fail
            }

            Program.options.folder_working = us.WorkingFolder;
            Globals.guiGraphWindowTopDistance = us.GraphWindowTopDistance;
            Globals.guiGraphWindowLeftDistance = us.GraphWindowLeftDistance;
            Globals.guiDecompWindowTopDistance = us.DecompWindowTopDistance;
            Globals.guiDecompWindowLeftDistance = us.DecompWindowLeftDistance;
            Globals.guiErrorWindowTopDistance = us.ErrorWindowTopDistance;
            Globals.guiErrorWindowLeftDistance = us.ErrorWindowLeftDistance;

            return us;
        }

        private static void GuiUpdateRecentFilesMenu()
        {
            try
            {

                List<string> liste = GetListFromCache();

                for (int i = 0; i < liste.Count; i++)
                {
                    if (i == 0) gui.toolStripMenuItem3.Text = "1. " + liste[i];
                    if (i == 1) gui.toolStripMenuItem4.Text = "2. " + liste[i];
                    if (i == 2) gui.toolStripMenuItem5.Text = "3. " + liste[i];
                    if (i == 3) gui.toolStripMenuItem6.Text = "4. " + liste[i];
                    if (i == 4) gui.toolStripMenuItem7.Text = "5. " + liste[i];
                    if (i == 5) gui.toolStripMenuItem8.Text = "6. " + liste[i];
                    if (i == 6) gui.toolStripMenuItem9.Text = "7. " + liste[i];
                    if (i == 7) gui.toolStripMenuItem10.Text = "8. " + liste[i];
                    if (i == 8) gui.toolStripMenuItem11.Text = "9. " + liste[i];
                    if (i == 9) gui.toolStripMenuItem12.Text = "10. " + liste[i];
                    if (i == 10) gui.toolStripMenuItem14.Text = "11. " + liste[i];
                    if (i == 11) gui.toolStripMenuItem15.Text = "12. " + liste[i];
                    if (i == 12) gui.toolStripMenuItem16.Text = "13. " + liste[i];
                    if (i == 13) gui.toolStripMenuItem17.Text = "14. " + liste[i];
                    if (i == 14) gui.toolStripMenuItem18.Text = "15. " + liste[i];
                    if (i == 15) gui.toolStripMenuItem19.Text = "16. " + liste[i];
                    if (i == 16) gui.toolStripMenuItem20.Text = "17. " + liste[i];
                    if (i == 17) gui.toolStripMenuItem21.Text = "18. " + liste[i];
                    if (i == 18) gui.toolStripMenuItem22.Text = "19. " + liste[i];
                    if (i == 19) gui.toolStripMenuItem23.Text = "20. " + liste[i];
                }
            }
            catch (Exception e)
            {
                //do nothing
            }
        }

        private static List<string> GetListFromCache()
        {
            List<string> liste = new List<string>();
            for (int i = Globals.guiRecentFoldersCache.priority.Count - 1; i >= 0; i--)
            {
                string item = (string)Globals.guiRecentFoldersCache.priority[i];
                liste.Add(item);
            }
            return liste;
        }
        /// <summary>
        /// Saves the user settings to xml file for later use.
        /// The file can be moved among different computers, in
        /// order to reuse the settings.
        /// </summary>
        /// <param name="us">UserSettings object</param>
        private static void SaveUserSettings(UserSettings us)
        {
            //save window position in user settings for next time, if the window is not maxi- or minimized
            if (gui.WindowState == FormWindowState.Normal)
            {
                us.MainWindowWidth = gui.Size.Width;
                us.MainWindowHeight = gui.Size.Height;
                us.MainWindowTopDistance = gui.Top;
                us.MainWindowLeftDistance = gui.Left;
                us.MainWindowSplitterDistance = gui.splitContainer1.SplitterDistance;
            }
            //Save user settings in xml file
            us.WorkingFolder = Program.options.folder_working;
            us.GraphWindowTopDistance = Globals.guiGraphWindowTopDistance;
            us.GraphWindowLeftDistance = Globals.guiGraphWindowLeftDistance;
            us.DecompWindowTopDistance = Globals.guiDecompWindowTopDistance;
            us.DecompWindowLeftDistance = Globals.guiDecompWindowLeftDistance;
            us.ErrorWindowTopDistance = Globals.guiErrorWindowTopDistance;
            us.ErrorWindowLeftDistance = Globals.guiErrorWindowLeftDistance;

            try
            {
                List<string> liste = GetListFromCache();
                for (int i = 0; i < liste.Count; i++)
                {
                    if (i == 0) us.RecentFolders1 = liste[i];
                    if (i == 1) us.RecentFolders2 = liste[i];
                    if (i == 2) us.RecentFolders3 = liste[i];
                    if (i == 3) us.RecentFolders4 = liste[i];
                    if (i == 4) us.RecentFolders5 = liste[i];
                    if (i == 5) us.RecentFolders6 = liste[i];
                    if (i == 6) us.RecentFolders7 = liste[i];
                    if (i == 7) us.RecentFolders8 = liste[i];
                    if (i == 8) us.RecentFolders9 = liste[i];
                    if (i == 9) us.RecentFolders10 = liste[i];

                    if (i == 10) us.RecentFolders11 = liste[i];
                    if (i == 11) us.RecentFolders12 = liste[i];
                    if (i == 12) us.RecentFolders13 = liste[i];
                    if (i == 13) us.RecentFolders14 = liste[i];
                    if (i == 14) us.RecentFolders15 = liste[i];
                    if (i == 15) us.RecentFolders16 = liste[i];
                    if (i == 16) us.RecentFolders17 = liste[i];
                    if (i == 17) us.RecentFolders18 = liste[i];
                    if (i == 18) us.RecentFolders19 = liste[i];
                    if (i == 19) us.RecentFolders20 = liste[i];
                }
            }
            catch
            {
                //ok if it fails
            }

            us.Save();
        }

        [STAThread]
        private void commands_Load(object sender, EventArgs e)
        {
            //gui.textBox2.Focus();
        }

        private void pIZToolStripMenuItem_Click(object sender, EventArgs e)
        {
            String dataFile = "";
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = Globals.userSettings.TspUtilityPath;
            openFileDialog1.Filter = "All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if ((openFileDialog1.OpenFile()) != null)
                    {
                        dataFile = openFileDialog1.FileName;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                }
            }

            if (dataFile == null) return;

            using (FileStream fs = Program.WaitForFileStream(Program.options.folder_working + "\\" + dataFile, Program.GekkoFileReadOrWrite.Read))
            using (StreamReader sr = new StreamReader(fs))
            {
                String input = sr.ReadToEnd();
                String output = Program.EncryptDecrypt(input);
                using (FileStream fs2 = Program.WaitForFileStream(dataFile + "1", Program.GekkoFileReadOrWrite.Read))
                using (StreamWriter sw = G.GekkoStreamWriter(fs2))
                {
                    sw.Write(output);
                    sw.Flush();
                    sw.Close();
                }
            }
        }

        private void radioButton1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
        }

        private void textBox2_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {

        }

        private void setWorkingDirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string folder = "";
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.SelectedPath = Program.options.folder_working;
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                folder = fbd.SelectedPath;
            }
            if (folder != "")
            {
                Program.options.folder_working = folder;
                System.IO.Directory.SetCurrentDirectory(Program.options.folder_working);
            }

            ChangeWorkingFolder(folder);

            Globals.userSettings.Save();
        }

        public static void ChangeWorkingFolder(string folder)
        {
            if (folder.StartsWith("1. ")) folder = folder.Substring(3);
            if (folder.StartsWith("2. ")) folder = folder.Substring(3);
            if (folder.StartsWith("3. ")) folder = folder.Substring(3);
            if (folder.StartsWith("4. ")) folder = folder.Substring(3);
            if (folder.StartsWith("5. ")) folder = folder.Substring(3);
            if (folder.StartsWith("6. ")) folder = folder.Substring(3);
            if (folder.StartsWith("7. ")) folder = folder.Substring(3);
            if (folder.StartsWith("8. ")) folder = folder.Substring(3);
            if (folder.StartsWith("9. ")) folder = folder.Substring(3);
            if (folder.StartsWith("10. ")) folder = folder.Substring(4);
            if (folder.StartsWith("11. ")) folder = folder.Substring(4);
            if (folder.StartsWith("12. ")) folder = folder.Substring(4);
            if (folder.StartsWith("13. ")) folder = folder.Substring(4);
            if (folder.StartsWith("14. ")) folder = folder.Substring(4);
            if (folder.StartsWith("15. ")) folder = folder.Substring(4);
            if (folder.StartsWith("16. ")) folder = folder.Substring(4);
            if (folder.StartsWith("17. ")) folder = folder.Substring(4);
            if (folder.StartsWith("18. ")) folder = folder.Substring(4);
            if (folder.StartsWith("19. ")) folder = folder.Substring(4);
            if (folder.StartsWith("20. ")) folder = folder.Substring(4);
            if (folder == "") return;

            if (!G.IsUnitTesting()) Globals.userSettings.WorkingFolder = folder;

            Program.options.folder_working = folder;
            if (G.SetWorkingFolder(false))
            {
                //folder does not exist.
            }
            else
            {
                G.Writeln();
                G.WriteDirs("small", false);

                if (!G.IsUnitTesting())
                {
                    Globals.guiRecentFoldersCache[Program.options.folder_working] = Program.options.folder_working;
                    GuiUpdateRecentFilesMenu();
                }
                ChangeWorkingFolderNoteMessage();
            }
        }

        private static void ChangeWorkingFolderNoteMessage()
        {
            G.Writeln("+++ NOTE: You may consider a RESTART (or RESET) command now. A RESTART command will");
            G.Writeln("          clear the workspace and run any INI file '" + Globals.autoExecCmdFileName + "' in the");
            G.Writeln("          newly chosen working folder.");
        }

        private void textBox2_VisibleChanged(object sender, EventArgs e)
        {
            //In order to move the focus to the command input textbox
            //Otherwise the focus is set on the results text box (above)
            gui.textBox2.Focus();
            gui.textBox2.Select();
        }

        private void Gekko_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.M)
            {
                CrossThreadStuff.SetTab("main", true);
            }
            else if (e.Control && e.KeyCode == Keys.O)
            {
                CrossThreadStuff.SetTab("output", true);
            }
            else if (e.Control && e.KeyCode == Keys.H)
            {
                CrossThreadStuff.SetTab("help", true);
            }
            else if (e.Control && e.KeyCode == Keys.U)
            {
                CrossThreadStuff.SetTab("menu", true);
            }
        }



        private void openDatabankToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void saveDatabankToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }


        //private void enableUndoForSIMToolStripMenuItem_Click(object sender, EventArgs e)
        //{
        //    Program.options.solve_undo = Program.options.solve_undo ? !Program.options.solve_undo : Program.options.solve_undo;
        //}

        private void makebatFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //dlgSaveFile is a SaveFileDialog object created from the toolbox
            //like mydoc0.txt

            GuiDialogMakeBatfile xx = new GuiDialogMakeBatfile(false);
            xx.ShowDialog();
            xx.Close();


        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            G.WriteDirs("large", false);
        }

        private void restoreUserSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {

            string warning = "";
            warning = "Reset all user settings to factory settings? \n ";
            warning += "(I.e. like when the software was started for the first time). \n ";
            warning += "At the moment this concerns only window positions, and user directory. \n ";
            warning += "Please note that this action cannot be undone. \n ";
            warning += "You will need to stop and restart Gekko afterwards. \n ";
            DialogResult dr = MessageBox.Show(warning, "Reset settings", MessageBoxButtons.YesNo);
            if (dr == DialogResult.Yes)
            {
                Globals.userSettings.Reset();
                Globals.doNotSaveUserSettings = true;
                MessageBox.Show("User settings are reset. You will need to stop and restart \nGekko in order for the changes to be effectuated.", "", MessageBoxButtons.OK);

            }

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
        }

        //found on the net, works much better than moving caret (--> flicker)
        [DllImport("User32.dll", CharSet = CharSet.Auto, EntryPoint = "SendMessage")]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        public void ScrollToEnd(RichTextBox rtb)
        {
            const int WM_VSCROLL = 277;
            const int SB_BOTTOM = 7;
            IntPtr ptrWparam = new IntPtr(SB_BOTTOM);
            IntPtr ptrLparam = new IntPtr(0);
            SendMessage(rtb.Handle, WM_VSCROLL, ptrWparam, ptrLparam);
        }

        private void textBox1_LinkClicked(object sender, System.Windows.Forms.LinkClickedEventArgs e)
        {
            LinkClicked(e, ETabs.Main);
        }
        private void textBoxTab2_LinkClicked(object sender, System.Windows.Forms.LinkClickedEventArgs e)
        {
            LinkClicked(e, ETabs.Output);
        }
        private void textBoxTab3_LinkClicked(object sender, System.Windows.Forms.LinkClickedEventArgs e)
        {
            LinkClicked(e, ETabs.Help);
        }

        private static void LinkClicked(System.Windows.Forms.LinkClickedEventArgs e, ETabs tab)
        {
            try
            {

                string input0 = e.LinkText;

                string type = null;
                string input = null;
                if (input0.Contains(Globals.linkSeparator1))
                {
                    int pos = input0.IndexOf(Globals.linkSeparator1);
                    string input2 = input0.Substring(pos + 1);
                    string[] input3 = input2.Split(Globals.linkSeparator2);
                    if (input3.Length != 2) throw new Exception("*** ERROR: strange error rgd. links");
                    type = input3[0];
                    input = input3[1];
                }
                else
                {
                    //simple rtf link
                    input = input0;
                    type = "help";
                }

                if (type == "tab")
                {
                    //for instance input0 = "output#tab:output"
                    if (input == "main") Gui.gui.tabControl1.SelectedTab = Gui.gui.tabPage1;
                    else if (input == "output") Gui.gui.tabControl1.SelectedTab = Gui.gui.tabPage2;
                    else if (input == "help") Gui.gui.tabControl1.SelectedTab = Gui.gui.tabPage3;
                    else if (input.Contains("output"))  //for instance "output12b"
                    {
                        string input2 = input.Substring(6);
                        string letter = input2.Substring(input2.Length - 1);
                        string number = input2.Substring(0, input2.Length - 1);
                        if (Globals.outputTabTextContainer.ContainsKey(number))
                        {
                            Program.ErrorContainer ec = Globals.outputTabTextContainer[number];
                            List<string> liste = null;
                            if (letter == "a") liste = ec.simInitEndoMissingValue;
                            else if (letter == "b") liste = ec.simNonExistingVariable;
                            else if (letter == "c") liste = ec.simMissingValueExoOrLaggedEndo;
                            Gui.gui.tabControl1.SelectedTab = Gui.gui.tabPage2;
                            Program.Cls("output");
                            foreach (string s in liste)
                            {
                                G.Writeln(s, ETabs.Output);
                            }
                        }
                    }
                    else throw new Exception("*** ERROR: strange error rgd. links");
                }
                else if (type == "disp")
                {
                    Globals.guiHomeMainEnabled = true;
                    //disp link, non-shown frn type eq
                    //for instance input0 = "fY#disp:fY"
                    List<string> temp = new List<string>();
                    temp.Add(input);
                    if (Globals.dispLastDispStart.super == 0)
                    {
                    }
                    GekkoTime t1 = Globals.dispLastDispStart;
                    GekkoTime t2 = Globals.dispLastDispEnd;
                    if (t1.super == 0)
                    {
                        t1 = Globals.globalPeriodStart;
                        t2 = Globals.globalPeriodEnd;
                    }
                    Program.Disp(t1, t2, temp, false, false, true, null);
                }
                else if (type == "disp2")
                {
                    Globals.guiHomeMainEnabled = true;
                    //disp link, shown frn type eq
                    //for instance input0 = "fY#disp2:fY"
                    List<string> temp = new List<string>();
                    temp.Add(input);
                    Program.Disp(Globals.dispLastDispStart, Globals.dispLastDispEnd, temp, true, false, true, null);
                }
                else if (type == "disp3")
                {
                    Globals.guiHomeMainEnabled = true;
                    //disp link, shown frn type eq
                    //for instance input0 = "fY#disp3:fY"
                    List<string> temp = new List<string>();
                    temp.Add(input);
                    Program.Disp(Globals.dispLastDispStart, Globals.dispLastDispEnd, temp, false, true, true, null);
                }
                else if (type == "help")
                {
                    //for instance input0 = "simulate#help:sim"
                    if (input == "<null>")
                    {
                        //do nothing -- this link is dead because of problems with the hidden text
                        //in the rtf file (the hidden text may be missing).
                        MessageBox.Show("*** ERROR: This link is not working because the link address is not given \nNormally the link address is provided as hidden text in the help file.");

                    }
                    else
                    {
                        Program.Help(input);
                    }
                }
                else if (type == "stacktrace")
                {
                    string s = "This error output is of technical character, and hence not very informative, unless you are a" + G.NL;
                    s += "programmer. You may click the 'Copy text' button, and then send the message text to the" + G.NL;
                    s += "Gekko editor in order to have the bug fixed/evalutated." + G.NL;
                    s += "-----------------------------------------------------------------------------------------------------------------------\n";
                    s += G.NL;
                    s += "Version: " + Globals.gekkoVersion + G.NL;
                    s += Globals.linkContainer[long.Parse(input)].s;
                    s += G.NL;
                    s += G.NL;
                    s += G.WriteDirs("large", true);
                    //MessageBox.Show(s);
                    WindowMessageBox w = new WindowMessageBox();
                    //w.textBox1.FontFamily = new System.Windows.Media.FontFamily("Courier New"); ugly...
                    w.textBox1.Text = s;
                    w.ShowDialog();
                }
                else if (type == "outputtab")
                {
                    Gui.gui.tabControl1.SelectedTab = Gui.gui.tabPage2;
                    Program.Cls("output");
                    string s = Globals.linkContainer[long.Parse(input)].s;
                    List<string> ss = G.ExtractLinesFromText(s);
                    foreach (string s2 in ss)
                    {
                        G.Writeln(s2, ETabs.Output);
                    }
                }
                else if (type == "undosim")
                {
                    if (!G.equal(Globals.undoSim.id.ToString(), input))
                    {
                        G.Writeln();
                        G.Writeln("*** ERROR: You can only undo the last simulation, not previous ones");
                        return;
                    }
                    else
                    {
                        Program.FromAToDatabank(Globals.undoSim.tStart, Globals.undoSim.tEnd, false, Program.databanks.GetFirst(), Globals.undoSim.obsWithLags, Globals.undoSim.obsSimPeriod, Globals.undoSim.a, null, null);
                        G.Writeln();
                        G.Writeln("Restored pre-simulation values for the period " + Globals.undoSim.tStart.ToString() + "-" + Globals.undoSim.tEnd.ToString());
                    }
                }
                else if (type == "packsim")
                {
                    if (!G.equal(Globals.packSim.id.ToString(), input))
                    {
                        G.Writeln();
                        G.Writeln("*** ERROR: You can only pack the last simulation, not previous ones");
                        return;
                    }
                    else
                    {
                        G.Writeln();
                        G.Writeln("Starting to pack zip file...");

                        Program.FromAToDatabank(Globals.packSim.tStart, Globals.packSim.tEnd, false, Program.databanks.GetFirst(), Globals.packSim.obsWithLags, Globals.packSim.obsSimPeriod, Globals.packSim.a, null, null);
                        Zipper zipper = new Zipper("gekko_sim_error.zip");

                        Program.Write(Program.databanks.GetFirst(), Globals.packSim.tStart0, Globals.packSim.tEnd, zipper.tempFolder + "\\bank", false, new List<BankNameVersion>(), "" + Globals.extensionDatabank + "", true, false);
                        Program.WaitForFileCopy(Globals.modelPathAndFileName, zipper.tempFolder + "\\model.frm"); ;
                        Program.Pipe(zipper.tempFolder + "\\simerror.txt", null);
                        G.Writeln(Globals.packSim.tStart.ToString() + " " + Globals.packSim.tEnd.ToString());
                        Program.PrintEndoExoLists();
                        Program.options.Write();
                        Program.Pipe("con", null);
                        zipper.ZipAndCleanup();

                        G.Writeln();
                        G.Writeln("Packed simulation error report into file 'gekko_sim_error.zip' in the working folder");
                        G.Writeln("You may send the zip file to the Gekko editor for evaluation (and possible fixing)");
                    }
                }
                else if (type == "list")
                {
                    Program.List(input, null, null);
                }
                else
                {
                    G.Writeln2("*** ERROR: strange error rgd. links");
                    throw new GekkoException();
                }
            }
            catch
            {
                Gui.gui.tabControl1.SelectedTab = Gui.gui.tabPage1;
                G.Writeln2("*** ERROR: problem with link");
                //consume this error: otherwise the whole GUI will close (for instance if a DISP-variable does not exist)
            }
        }


        [STAThread]
        public void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (Gui.gui.tabControl1.SelectedIndex == 0)
            {
                if (Program.guiBrowseNumber < 2) return;
                string var = Program.guiBrowseHistory[Program.guiBrowseNumber - 2];
                Program.guiBrowseNumber += -2;  //1 will be added later, when calling "disp". Net result: -1.

                if (Program.model.varsAType.ContainsKey(var))
                {
                    List<string> temp = new List<string>();
                    temp.Add(var);
                    Program.Disp(Globals.dispLastDispStart, Globals.dispLastDispEnd, temp, false, false, true, null);
                    //Program.guiBrowseHistory.Add(e.LinkText);
                }
                gui.textBox2.Focus();
            }
            //else if (Gui.gui.tabControl1.SelectedIndex == 2)
            //{
            //    if (Program.guiBrowseHelpNumber < 2) return;
            //    string var = Program.guiBrowseHelpHistory[Program.guiBrowseHelpNumber - 2];
            //    Program.guiBrowseHelpNumber += -2;  //1 will be added later, when calling "help". Net result: -1.
            //    Program.Help(var);
            //    gui.textBoxTab3.Focus();
            //}
            else if (Gui.gui.tabControl1.SelectedIndex == 2)
            {
                //if (gui.webBrowser.CanGoBack && Globals.webBrowserHistoryCounterTotal > Globals.webBrowserHistoryCounterTotalStart)
                if (gui.webBrowser.CanGoBack)
                {
                    gui.webBrowser.GoBack();
                    //Globals.webBrowserHistoryCounterTotal += -2;
                }
            }
        }

        public void toolStripButton2_Click(object sender, EventArgs e)
        {
            if (Gui.gui.tabControl1.SelectedIndex == 0)
            {
                if (Program.guiBrowseNumber >= Program.guiBrowseHistory.Count) return;
                string var = Program.guiBrowseHistory[Program.guiBrowseNumber - 0];
                Program.guiBrowseNumber += 0;  //1 will be added later, when calling "disp". Net result: 1.
                if (Program.model.varsAType.ContainsKey(var))
                {
                    List<string> temp = new List<string>();
                    temp.Add(var);
                    Program.Disp(Globals.dispLastDispStart, Globals.dispLastDispEnd, temp, false, false, true, null);
                }
                gui.textBox2.Focus();
            }
            //else if (Gui.gui.tabControl1.SelectedIndex == 2)
            //{
            //    if (Program.guiBrowseHelpNumber >= Program.guiBrowseHelpHistory.Count) return;
            //    string var = Program.guiBrowseHelpHistory[Program.guiBrowseHelpNumber - 0];
            //    Program.guiBrowseHelpNumber += 0;  //1 will be added later, when calling "disp". Net result: 1.
            //    Program.Help(var);
            //    gui.textBoxTab2.Focus();
            //}
            else if (Gui.gui.tabControl1.SelectedIndex == 2)
            {
                //if (gui.menu1.webBrowser.CanGoForward)
                //{
                //    gui.menu1.webBrowser.GoForward();
                //}
                if (gui.webBrowser.CanGoForward)
                {
                    gui.webBrowser.GoForward();
                }
            }
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            gui.textBox2.Focus();
        }

        private void comparecheckDatabanksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                CompareUtilityDatabanks gui = new CompareUtilityDatabanks();
                gui.ShowDialog();
                gui.Close();
            }
            catch
            {
                G.Writeln2("*** ERROR: Comparing failed");
                G.Writeln();
            }

        }

        private void comparecheckEquationsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                CompareUtilityEquations gui = new CompareUtilityEquations();
                gui.ShowDialog();
                gui.Close();
            }
            catch
            {
                G.Writeln2("*** ERROR: Comparing failed");
                G.Writeln();
            }
        }

        private void compareModeldatabankvarlistToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                CompareUtility3Way gui = new CompareUtility3Way();
                gui.ShowDialog();
                gui.Close();
                if (Program.model != null) G.Writeln("Output is in file 'compare.txt'");
            }
            catch
            {
                G.Writeln2("*** ERROR: Comparing failed");
                G.Writeln();
            }
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
                CrossThreadStuff.Blink();
        }

        public void StartThread(string s, bool newUserInput)
        {
            toolStripStatusLabel3.Image = yellow;
            toolStripButton3.Enabled = true;
            Globals.dateStamp = Program.GetDateStamp();  //takes a small amount of time to generate, so we put it in globally for later use in SERIES statements etc. Around midnight, this may be 1 day off.....!

            //System.Threading.Timer timer = new System.Threading.Timer(new TimerCallback(xx), null, 0, 1000);

            //Blinking icon
            Globals.guiTimerCounter = 0;
            if (Globals.aTimer == null)
            {
                Globals.aTimer = new System.Timers.Timer();
                Globals.aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
                Globals.aTimer.Interval = 500;
            }
            Globals.aTimer.Stop();
            Globals.aTimer.Start();

            //TODO: Really this stuff should be stored in the P object, instead of here
            Globals.numberOfErrors = 0;
            Globals.numberOfWarnings = 0;
            Globals.numberOfSkippedLines = 0;
            Globals.threadIsInProcessOfAborting = false;  //clearing this
            Globals.applicationIsInProcessOfAborting = false;  //clearing this
            Globals.errorMemory = null;  //so that it is not recording all the time.            

            if (newUserInput)
            {
                Globals.tasks.Enqueue(s);
                if (Globals.tasks.Count > 1)  //if 0 just run it normally
                {
                    return;  //The job is dealt with in ThreadFinished()
                }
                else
                {
                    //Just proceed normally
                }
            }
            else
            {
                //Just run the queued job as if it was entered on the keyboard
            }

            Globals.btnStartThread = false;
            Globals.btnStopThread = true;
            // reset events
            gui.threadEventStopThread.Reset();
            gui.threadEventThreadStopped.Reset();
            // create worker thread instance
            gui.threadInput = s;
            gui.threadWorkerThread = new Thread(new ThreadStart(this.WorkerThreadFunction), 10000000);  //with 1.7 MB (1700000) we can a sum of about 120 variables. Augmenting to 10 MB (10000000) --> around 700 variables in the sum (if it is a simple sum). That ought to be enough and 10x the default of 1 MB. The problem is all due to recursion when walking the AST.
            gui.threadWorkerThread.SetApartmentState(ApartmentState.STA);
            gui.threadWorkerThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");  //gets . instead of , in doubles
            gui.threadWorkerThread.Start();
        }

        // Stop Thread button is pressed
        private void btnStopThread_Click(object sender, System.EventArgs e)
        {
            StopThread();
        }

        // Exit button is pressed.
        private void btnExit_Click(object sender, System.EventArgs e)
        {
            this.Close();
        }

        // Form is closed.
        // Stop thread if it is active.
        private void MainForm_Closed(object sender, System.EventArgs e)
        {
            StopThread();
        }

        // Worker thread function.
        public void WorkerThreadFunction()
        {
            this.p = new P();  //keeps track of where thread is
            LongProcess longProcess;
            longProcess = new LongProcess(threadEventStopThread, threadEventThreadStopped, this);
            if (Globals.testVersion)
            {
                longProcess.Run(p);
            }
            else
            {

                {
                    string commandLine = longProcess.gekkoGui.threadInput;

                    if (true)
                    {

                        try
                        {
                            longProcess.Run(p);
                        }
                        catch (Exception e2)
                        {
                            Program.PrintExceptionAndFinishThread(e2, p);
                            if (!Globals.applicationIsInProcessOfAborting)
                            {
                                Globals.aTimer.Stop();  //otherwise it will blink on
                                Gui.gui.toolStripStatusLabel3a.Text = " ";
                                toolStripStatusLabel3.Image = red;
                                toolStripButton3.Enabled = false;
                            }
                            Program.GekkoExceptionCleanup(p);
                        }
                    }
                }
            }
        }        

        // Stop worker thread if it is running.
        // Called when user presses Stop button or form is closed.
        private void StopThread()
        {
            if (threadWorkerThread != null && threadWorkerThread.IsAlive)  // thread is active
            {
                // set event "Stop"
                threadEventStopThread.Set();

                // wait when thread  will stop or finish
                while (threadWorkerThread.IsAlive)
                {
                    // We cannot use here infinite wait because our thread
                    // makes syncronous calls to main form, this will cause deadlock.
                    // Instead of this we wait for event some appropriate time
                    // (and by the way give time to worker thread) and
                    // process events. These events may contain Invoke calls.
                    if (WaitHandle.WaitAll(
                        (new ManualResetEvent[] { threadEventThreadStopped }),
                        100,
                        true))
                    {
                        break;
                    }

                    Application.DoEvents();
                }
            }
            ThreadFinished();		// set initial state of buttons
        }

        // Add string to list box.
        // Called from worker thread using delegate and Control.Invoke
        public void AddString(Object o)
        {
            Program.WorkerThreadHelper2 wh = (Program.WorkerThreadHelper2)o;
            G.WriteAbstract2(o);  //has no link
        }

        // Called from worker thread using delegate and Control.Invoke
        public void SetTitleEtc(Object s)
        {
            Program.WorkerThreadHelper1 wh = (Program.WorkerThreadHelper1)s;
            if (wh.homeButton == "true")
                this.toolStripButton5.Enabled = true;
            if (wh.homeButton == "false") this.toolStripButton5.Enabled = false;
            if (wh.titleField != "NULL") this.Text = wh.titleField;
            if (wh.statusField != "NULL") this.toolStripStatusLabel1.Text = wh.statusField;
            //These 4 can also be  "NULL"
            if (wh.leftArrow == "true") this.toolStripButton1.Enabled = true;
            if (wh.leftArrow == "false") this.toolStripButton1.Enabled = false;
            if (wh.rightArrow == "true") this.toolStripButton2.Enabled = true;
            if (wh.rightArrow == "false") this.toolStripButton2.Enabled = false;
        }

        // Set initial state of controls.
        // Called from worker thread using delegate and Control.Invoke
        public void ThreadFinished()
        {
            if (Globals.applicationIsInProcessOfAborting) return;  //do not start up anything new now!

            Program.MaybePlaySound(p);

            Globals.btnStartThread = true;
            Globals.btnStopThread = false;
            Globals.tasks.Dequeue();

            //Console.WriteLine("tr�d stop: " + gui.m_WorkerThread.GetHashCode());
            if (Globals.tasks.Count > 0)
            {
                string s = Globals.tasks.Peek();  //this must be the buffer regarding command line input
                StartThread(s, false);
            }
            else
            {
                try
                {
                    Globals.aTimer.Stop();  //otherwise it will blink on
                    Gui.gui.toolStripStatusLabel3a.Text = " ";
                    toolStripStatusLabel3.Image = green;
                    int goals = 0;
                    if (Program.model != null) goals = Math.Max(Program.model.exogenized.Count, Program.model.endogenized.Count);
                    if (goals > 0)
                    {
                        this.toolStripStatusLabel2.Margin = new Padding(-5, 0, 0, -2);
                        toolStripStatusLabel5.Image = target;
                        this.toolStripStatusLabel2.Text = goals.ToString();
                    }
                    else
                    {
                        this.toolStripStatusLabel2.Margin = new Padding(0, 0, 0, 0);
                        toolStripStatusLabel5.Image = null;
                        this.toolStripStatusLabel2.Text = "";
                    }
                    toolStripButton3.Enabled = false;
                }
                catch
                {
                    //not the end of the world if green light is not set
                }
                p.ReportToRunStatus(true);
                Gui.PrintTotalErrors(p);
            }
        }

        public static void PrintTotalErrors(P p)
        {
            if (Globals.numberOfWarnings + Globals.numberOfErrors > 0)
            {
                G.Writeln();
                if (Globals.numberOfErrors > 0)
                {
                    if (Globals.numberOfErrors == 1) G.Writeln("There was " + Globals.numberOfErrors + " ERROR message while running the command");
                    else G.Writeln("There were " + Globals.numberOfErrors + " ERROR messages while running the command");
                }
                if (Globals.numberOfWarnings > 0)
                {
                    if (Globals.numberOfWarnings == 1) G.Writeln("There was " + Globals.numberOfWarnings + " WARNING message while running the command");
                    else G.Writeln("There were " + Globals.numberOfWarnings + " WARNING messages while running the command");
                }
                if (Globals.numberOfSkippedLines > 0)
                {
                    if (Globals.numberOfSkippedLines == 1) G.Writeln("There was " + Globals.numberOfSkippedLines + " SKIPPED LINE while running the command");
                    else G.Writeln("There were " + Globals.numberOfSkippedLines + " SKIPPED LINES while running the command");
                }
            }

            if (p.hasBeenCmdFile)
            {
                if (!(G.Contains(p.lastFileSentToANTLR, Globals.autoExecCmdFileName)))
                {
                    double ms = (DateTime.Now - p.startingTime).TotalMilliseconds;
                    if (ms > 1000) {  //to avoid UFunctions being shown here. Fix better when #980324532985 is done
                        G.Writeln();
                        G.Writeln("Total elapsed time: " + G.SecondsFormat(ms));
                        G.Writeln();
                    }
                }
            }
        }

        public void GuiBrowseArrowsStuff(string var, bool link, ETabs type)
        {
            //0: disp/equation browser
            //1: output
            //2: menu
            //if var == null, it means to refresh arrows with no new var entered (used when changing tabs)

            if (type == ETabs.Main)
            {
                if (var != null)  //a DISP, either via a command or link clicked
                {
                    if (Program.guiBrowseNumber > Program.guiBrowseHistory.Count - 1)
                    {
                        //enlarging history
                        if (!link)
                        {
                            Program.guiBrowseHistory.Clear();
                            Program.guiBrowseNumber = 0;
                        }
                        Program.guiBrowseHistory.Add(var);
                    }
                    else
                    {
                        if (link && G.equal(var, Program.guiBrowseHistory[Program.guiBrowseNumber]))
                        {
                            //retracing old history, but only if link (otherwise it will be treated as a DISP command, starting from scratch)
                        }
                        else
                        {
                            if (!link)
                            {
                                Program.guiBrowseHistory.Clear();
                                Program.guiBrowseNumber = 0;
                            }
                            else
                            {
                                //invalidating sub-history and adding new item
                                int dif = Program.guiBrowseHistory.Count - Program.guiBrowseNumber;
                                for (int i2 = 0; i2 < dif; i2++)
                                {
                                    Program.guiBrowseHistory.RemoveAt(Program.guiBrowseHistory.Count - 1);
                                }
                            }
                            Program.guiBrowseHistory.Add(var);
                        }
                    }
                    Program.guiBrowseNumber++;
                }
            }
            else if (type == ETabs.Help)
            {
                //this can be deleted
                if (var != null)
                {
                    if (Program.guiBrowseHelpNumber > Program.guiBrowseHelpHistory.Count - 1)
                    {
                        Program.guiBrowseHelpHistory.Add(var);
                    }
                    else
                    {
                        if (G.equal(var, Program.guiBrowseHelpHistory[Program.guiBrowseHelpNumber]))
                        {
                        }
                        else
                        {
                            int dif = Program.guiBrowseHelpHistory.Count - Program.guiBrowseHelpNumber;
                            for (int i2 = 0; i2 < dif; i2++)
                            {
                                Program.guiBrowseHelpHistory.RemoveAt(Program.guiBrowseHelpHistory.Count - 1);
                            }
                            Program.guiBrowseHelpHistory.Add(var);
                        }
                    }
                    Program.guiBrowseHelpNumber++;
                }
                Program.WorkerThreadHelper1 wh = new Program.WorkerThreadHelper1();
                if (Program.guiBrowseHelpNumber >= 2) wh.leftArrow = "true";
                else wh.leftArrow = "false";
                if (Program.guiBrowseHelpNumber < Program.guiBrowseHelpHistory.Count) wh.rightArrow = "true";
                else wh.rightArrow = "false";

                Gui gui = Gui.gui;
                if (gui != null && gui.IsHandleCreated)  //Invoke can not be called if there is no handle created
                {
                    gui.Invoke(gui.threadDelegateSetTitle, wh);
                }
            }
            else if (type == ETabs.Output)
            {
            }
            else if (type == ETabs.Menu)
            {
            }

            CrossThreadStuff.RefreshArrowsEtc();
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ETabs type = 0;
            if (Gui.gui.tabControl1.SelectedIndex == 0) type = ETabs.Main;
            else if (Gui.gui.tabControl1.SelectedIndex == 1) type = ETabs.Output;
            //else if (Gui.gui.tabControl1.SelectedIndex == 2) type = ETabs.Help;
            else if (Gui.gui.tabControl1.SelectedIndex == 2)
            {
                type = ETabs.Menu;
                //if (this.menu1.webBrowser.Source == null)
                if (this.webBrowser.Url == null)
                {
                    CrossThreadStuff.RestartMenuBrowser();
                }
            }
            GuiBrowseArrowsStuff(null, false, type);
        }


        //[DllImport("wininet.dll")]
        //private extern static bool InternetSetOption(int hInternet,
        //int dwOption,
        //ref INTERNET_CONNECTED_INFO lpBuffer,
        //int dwBufferLength
        //);

        //[StructLayout(LayoutKind.Sequential)]
        //struct INTERNET_CONNECTED_INFO
        //{
        //    public int dwConnectedState;
        //    public int dwFlags;
        //} ;

        ///// <summary>
        ///// flush Credentials
        ///// </summary>
        //static public int flushCredentials()
        //{
        //    INTERNET_CONNECTED_INFO ci = new INTERNET_CONNECTED_INFO();

        //    InternetSetOption(0, 42, ref ci, Marshal.SizeOf(ci));
        //    return 0;
        //}


        public void RestartMenuBrowser()
        {
            //Globals.webBrowserHistoryCounterTotalStart = Globals.webBrowserHistoryCounterTotal;

            List<string> folders = new List<string>();
            folders.Add(Program.options.folder_menu); //looks here first, after looking in working folder

            string fileName = "";
            if (Program.options.menu_startfile != "")
            {
                fileName = Program.options.menu_startfile;
                fileName = Program.AddExtension(fileName, ".html");
            }
            else
            {
                fileName = "menu.html";
            }
            string orignialFileName = fileName;

            fileName = Program.SearchForFile(fileName, folders);  //calls CreateFullPathAndFileName()

            if (File.Exists(fileName))
            {
                string path = Path.GetDirectoryName(fileName);
                string menuFolder = Program.CreateFullPathAndFileName(Program.options.folder_menu);

                if (G.IsSamePath(Program.options.folder_working, path) || G.IsSamePath(menuFolder, path))
                {
                    //We will only suggest copying in a missing css/png file if it is in one of these folders
                    //TODO: should be done a bit smarter later on, perhaps by parsing the html file to see if the css/png
                    //      file will end up missing.

                    if (!File.Exists(path + "\\" + "styles.css"))
                    {
                        if (MessageBox.Show("Menu helper file missing.\nIt seems the file 'styles.css' is missing: would you like to add it to the \nfolder '" + path + "'?\n(This is completely safe and will improve the menu layout)", "Missing file", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            try
                            {
                                string stylesFile = Application.StartupPath + "\\images\\" + "styles.css";
                                Program.WaitForFileCopy(stylesFile, path + "\\styles.css");
                            }
                            catch
                            {
                                G.Writeln2("*** ERROR: Could not copy styles.css from Gekko program folder");
                                throw new GekkoException();
                            }
                        }
                    }
                    if (!File.Exists(path + "\\" + "table.png"))
                    {
                        if (MessageBox.Show("Menu helper file missing.\nIt seems the file 'table.png' is missing: would you like to add it to the \nfolder '" + path + "'?\n(This is completely safe and will improve the menu layout)", "Missing file", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            try
                            {
                                string tableIconFile = Application.StartupPath + "\\images\\" + "table.png";
                                Program.WaitForFileCopy(tableIconFile, path + "\\table.png");
                            }
                            catch
                            {
                                G.Writeln2("*** ERROR: Could not copy styles.css from Gekko program folder");
                                throw new GekkoException();
                            }
                        }
                    }
                }
                this.webBrowser.Url = new Uri("file:///" + fileName);
                //Globals.webBrowserHistoryCounterTotal += 1;  <--- no, not here

            }
            else
            {
                if (false)
                {
                    this.webBrowser.Url = new Uri("file:///" + "c:\\lkasd\\lkajsdf.sad");
                }
                else
                {
                    //string s = Program.options.menu_startfile;
                    //this.webBrowser.Navigate("about:blank");
                    //HtmlDocument doc = this.webBrowser.Document;
                    //doc.Write("<html><body><FONT FACE= \"Courier New\"><FONT SIZE=2 color=\"red\"><p>" + "*** ERROR: Could not find start menu file (" + orignialFileName + ")" + "<p>" + "The file menu.html must be available in working or menu folder ('OPTION folder menu = ...')" + "</p></body></html>");
                    this.webBrowser.DocumentText = "<html><body><FONT FACE= \"Courier New\"><FONT SIZE=2 color=\"red\"><p>" + "*** ERROR: Could not find start menu file (" + orignialFileName + ")" + "<p>" + "The menu file must be available in working or menu folder ('OPTION folder menu = ...')" + "</p></body></html>";
                }
            }
        }

        private void tabPage2_Click(object sender, EventArgs e)
        {

        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.SendWait("^z");
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.SendWait("^y");
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.SendWait("^x");
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.SendWait("^c");
            //Clipboard.SetText(this.textBox2.SelectedText);
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.SendWait("^v");
            //this.textBox2.SelectedText = Clipboard.GetText();
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.SendWait("^a");
        }

        private void showCurrentOptionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Program.options.Write();
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            ChangeWorkingFolder(gui.toolStripMenuItem3.Text);
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            ChangeWorkingFolder(gui.toolStripMenuItem4.Text);
        }

        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {
            ChangeWorkingFolder(gui.toolStripMenuItem5.Text);
        }

        private void toolStripMenuItem6_Click(object sender, EventArgs e)
        {
            ChangeWorkingFolder(gui.toolStripMenuItem6.Text);
        }

        private void toolStripMenuItem7_Click(object sender, EventArgs e)
        {
            ChangeWorkingFolder(gui.toolStripMenuItem7.Text);
        }

        private void toolStripMenuItem8_Click(object sender, EventArgs e)
        {
            ChangeWorkingFolder(gui.toolStripMenuItem8.Text);
        }

        private void toolStripMenuItem9_Click(object sender, EventArgs e)
        {
            ChangeWorkingFolder(gui.toolStripMenuItem9.Text);
        }

        private void toolStripMenuItem10_Click(object sender, EventArgs e)
        {
            ChangeWorkingFolder(gui.toolStripMenuItem10.Text);
        }

        private void toolStripMenuItem11_Click(object sender, EventArgs e)
        {
            ChangeWorkingFolder(gui.toolStripMenuItem11.Text);
        }

        private void toolStripMenuItem12_Click(object sender, EventArgs e)
        {
            ChangeWorkingFolder(gui.toolStripMenuItem12.Text);
        }

        private void toolStripMenuItem14_Click(object sender, EventArgs e)
        {
            ChangeWorkingFolder(gui.toolStripMenuItem14.Text);
        }

        private void toolStripMenuItem15_Click(object sender, EventArgs e)
        {
            ChangeWorkingFolder(gui.toolStripMenuItem15.Text);
        }

        private void toolStripMenuItem16_Click(object sender, EventArgs e)
        {
            ChangeWorkingFolder(gui.toolStripMenuItem16.Text);
        }

        private void toolStripMenuItem17_Click(object sender, EventArgs e)
        {
            ChangeWorkingFolder(gui.toolStripMenuItem17.Text);
        }

        private void toolStripMenuItem18_Click(object sender, EventArgs e)
        {
            ChangeWorkingFolder(gui.toolStripMenuItem18.Text);
        }

        private void toolStripMenuItem19_Click(object sender, EventArgs e)
        {
            ChangeWorkingFolder(gui.toolStripMenuItem19.Text);
        }

        private void toolStripMenuItem20_Click(object sender, EventArgs e)
        {
            ChangeWorkingFolder(gui.toolStripMenuItem20.Text);
        }

        private void toolStripMenuItem21_Click(object sender, EventArgs e)
        {
            ChangeWorkingFolder(gui.toolStripMenuItem21.Text);
        }

        private void toolStripMenuItem22_Click(object sender, EventArgs e)
        {
            ChangeWorkingFolder(gui.toolStripMenuItem22.Text);
        }

        private void toolStripMenuItem23_Click(object sender, EventArgs e)
        {
            ChangeWorkingFolder(gui.toolStripMenuItem23.Text);
        }

        private void Gui_Shown(object sender, EventArgs e)
        {
            GuiAutoExecStuff();
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            GuiDialogMakeShortcut xx = new GuiDialogMakeShortcut();
            xx.ShowDialog();
            xx.Close();
        }

        private void toolStripButton3_Click_1(object sender, EventArgs e)
        {
            //StopThread();
            if (threadWorkerThread != null && threadWorkerThread.IsAlive == true)
            {
                G.Writeln("+++ NOTE: Trying to stop the job -- may take some time...", Color.Red);
                Globals.threadIsInProcessOfAborting = true;
            }
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {            
            Application.Exit();  //will go to #3452345523 now
        }        

        private void contextMenuStrip2_Opening(object sender, CancelEventArgs e)
        {
        }

        private void convertToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StringBuilder s = new StringBuilder();
            s.Append("This facility converts Gekko tables (.tab files) to text format, i.e. inserts Gekko-generated tables (with data taken from ");
            s.Append("databanks) instead of the original .tab files. In this way, these text files may be used together with html menu files, in a setting ");
            s.Append("without access to Gekko (for instance on the intra- or internet). In addition to this, the converter also modifies html menu files ");
            s.Append("slightly, so that they can handle the text table files.");
            s.AppendLine();
            s.AppendLine();
            s.Append("In order for this to work, you will have to first open up a databank ('READ [databank]'), and afterwards set global time ");
            s.Append("to the desired time period.");
            s.AppendLine();
            s.AppendLine();
            s.Append("If you click OK now, Gekko will ask you to point to a folder containing tables (and possibly menus). All .tab and .html files in the folder and sub-folders ");
            s.Append("will be converted, but note that the orignal files will not be changed in any way. The result of the conversion is put into ");
            s.Append("a zip file ('converted_table_text.zip') in the working folder.");

            DialogResult myDialogResult = MessageBox.Show(s.ToString(), "Convert .tab to text", MessageBoxButtons.OKCancel);
            if (myDialogResult == DialogResult.OK)
            {
                //Program.ConvertTabToText2();
                string folder = "";
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                fbd.SelectedPath = Program.options.folder_working;
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    folder = fbd.SelectedPath;
                }
                if (folder != "")
                {
                    RunGekkoTabToTextStuff(folder);
                }
                else
                {
                    G.Writeln2("*** ERROR: please choose a folder");
                }

            }
        }

        private void convertPCIMTablesToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            StringBuilder s = new StringBuilder();
            s.Append("This facility converts PCIM tables to the Gekko table format (XML). The converter works for simple tables, ");
            s.Append("That is, tables with only one section of variables (where code '01' is follwed by '1': for instance '01  1 70 70'). ");
            s.Append("The converter is not perfect, but in most cases the resulting table will resemble the old one pretty much as ");
            s.Append("regards layout etc. You may fine-tune the new table(s) afterwards, preferably in a XML editor.");
            s.AppendLine();
            s.AppendLine();
            s.Append("Gekko will now ask you to point to a folder containing the tables. All tables (*.tab) in all sub-folders ");
            s.Append("will be converted, and the result can be found in the file 'converted_tables.zip' in the working folder.");

            DialogResult myDialogResult = MessageBox.Show(s.ToString(), "Convert PCIM tables", MessageBoxButtons.OKCancel);
            if (myDialogResult == DialogResult.OK)
            {
                Program.ConvertTab2();
            }
        }

        private void convertPCIMMenusToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            StringBuilder s = new StringBuilder();
            s.Append("This facility converts PCIM menus to HTML format. PCIM menus are a collection of relatively simple .cmd files ");
            s.Append("calling each other. The converter will convert .cmd files in a particular folder you point it to, including any ");
            s.Append("subfolders. You may fine-tune the new HTML menus afterwards, preferably in a HTML editor ");
            s.Append("-- or in raw code if you are familiar with HTML syntax.");
            s.AppendLine();
            s.AppendLine();
            s.Append("Gekko will now ask you to point to a folder containing the menus. All *.cmd files in the folder and sub-folders ");
            s.Append("will be converted, and the result can be found in the file 'converted_menus.zip' in the working folder.");

            DialogResult myDialogResult = MessageBox.Show(s.ToString(), "Convert PCIM menus", MessageBoxButtons.OKCancel);
            if (myDialogResult == DialogResult.OK)
            {
                Program.ConvertMenu2();
            }
        }

        private void tSPImportEqsToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            TspUtilitiesGui gui = new TspUtilitiesGui();
            gui.ShowDialog();
            gui.Close();

            String dataFile = "";
            String templateFile = "";
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.Filter = "Template file (*.frm)|*.frm|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            //openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if ((openFileDialog1.OpenFile()) != null)
                    {
                        templateFile = openFileDialog1.FileName;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                }
            }

            openFileDialog1.InitialDirectory = Globals.userSettings.TspUtilityPath;
            openFileDialog1.Filter = "Tsp output (*.out)|*.out|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            //openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if ((openFileDialog1.OpenFile()) != null)
                    {
                        dataFile = openFileDialog1.FileName;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                }
            }


            if (templateFile == "")
            {
                G.Writeln2("*** ERROR in TSP import: nothing converted");
                return;
            };
            String dir = Path.GetDirectoryName(templateFile);
            String frmOutputFile = dir + Path.DirectorySeparatorChar + "output.frm";
            //String tsdOutputFile = dir + Path.DirectorySeparatorChar + "output.tsd";
            Globals.userSettings.TspUtilityPath = dir;

            bool ex1 = File.Exists(frmOutputFile);
            //bool ex2 = File.Exists(tsdOutputFile);

            String warning = "";
            if (ex1)
            {
                warning = "Warning: output.frm already exist in the chosen folder. Overwrite?";
                DialogResult dr = MessageBox.Show(warning, "Warning", MessageBoxButtons.YesNo);
                switch (dr)
                {
                    case DialogResult.No:
                        return;
                        break;
                }
            }
            TspUtilities.tspUtility(dataFile, templateFile, frmOutputFile);
        }

        private void tSPImportDataToolStripMenuItem1_Click_1(object sender, EventArgs e)
        {
            TspUtilitiesDataGui gui = new TspUtilitiesDataGui();
            gui.ShowDialog();
            gui.Close();

            String dataFile = "";
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = Globals.userSettings.TspUtilityPath;
            openFileDialog1.Filter = "All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            //openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if ((openFileDialog1.OpenFile()) != null)
                    {
                        dataFile = openFileDialog1.FileName;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                }
            }
            if (dataFile == "")
            {
                G.Writeln2("*** ERROR in TSP data import: nothing converted");
                return;
            }
            string dir = Path.GetDirectoryName(dataFile);
            string tsdOutputFile = dir + Path.DirectorySeparatorChar + "output.tsd";

            Globals.userSettings.TspUtilityPath = dir;

            bool ex1 = File.Exists(tsdOutputFile);
            if (ex1)
            {
                String warning = "Warning: TSD file already exist in the chosen folder. Overwrite?";
                DialogResult dr = MessageBox.Show(warning, "Warning", MessageBoxButtons.YesNo);
                switch (dr)
                {
                    case DialogResult.No:
                        return;
                        break;
                }

            }

            TspUtilities.tspDataUtility(dataFile, tsdOutputFile);
        }

        private void webBrowser_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            if (e.Url == null) return;            
            int i = e.Url.AbsoluteUri.ToLower().LastIndexOf("#");
            string c = null;
            if (i != -1)
            {
                string c2 = e.Url.AbsoluteUri.ToLower().Substring(i + "#".Length);
                if (Program.IsPrintCodeShort(c2)) c = c2;
            }


            if (c == null && (e.Url.AbsoluteUri.ToLower().EndsWith("." + Globals.extensionTable) || e.Url.AbsoluteUri.ToLower().EndsWith("." + "tab")))
            {
                //cancel Navigation
                string file = e.Url.AbsoluteUri;
                if (file.ToLower().StartsWith("http://") || file.ToLower().StartsWith("https://"))
                {
                    //This is an external link
                    MessageBox.Show("*** ERROR: Expected table file to be on local file system");
                    e.Cancel = true;
                }
                else
                {
                    file = file.Substring(8);
                    file = file.Replace("/", "\\");
                    Gui.gui.StartThread("menutable " + file + ";", true);  //to get a worker thread started
                    Globals.lastCalledMenuTable = file; //TODO: THIS CAN BE DELETED!!
                    //A bit hacky: but if table type is 'html' we should stay in Menu tab.
                    if (!G.equal(Program.options.table_type, "html")) CrossThreadStuff.SetTab("main", true);
                    e.Cancel = true;
                }
            }
            else if (c == null && (e.Url.AbsoluteUri.ToLower().EndsWith("." + Globals.extensionCommand)))
            {
                //Command file (gcm)
                //cancel Navigation
                string file = e.Url.AbsoluteUri;
                if (file.ToLower().StartsWith("http://") || file.ToLower().StartsWith("https://"))
                {
                    //This is an external link
                    MessageBox.Show("*** ERROR: Expected command file to be on local file system");
                    e.Cancel = true;
                }
                else
                {
                    file = file.Substring(8);
                    file = file.Replace("/", "\\");
                    string inputFileName = Path.GetFileName(file);
                    Gui.gui.StartThread("RUN " + file + ";", true);  //to get a worker thread started                    
                    CrossThreadStuff.SetTab("main", true);
                    e.Cancel = true;
                }
            }
            else if (c != null)
            {                
                Gui.gui.StartThread("menutable <" + c + "> " + Globals.lastCalledMenuTable + ";", true);
                e.Cancel = true;
            }
            else
            {
                string file = e.Url.AbsoluteUri;
                if (file.ToLower().StartsWith("http://") || file.ToLower().StartsWith("https://"))
                {
                    //This is an external link
                    //We do not allow external links to open inside the Menu tab, mainly for
                    //security reasons (even though the browser component is supposed to be
                    //the newest IE installed on the system).
                    //If the user wants to check Facebook, use a real browser! (from the Menu
                    //tab, you may click Ctrl-O to navigate).
                    e.Cancel = true;
                    Process.Start(file);
                }
            }
            Globals.guiHomeMenuEnabled = true;
        }

        private void webBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            this.webBrowser.Document.Body.Style += "; zoom:" + Program.options.interface_zoom + "%";
            Gui.gui.GuiBrowseArrowsStuff(null, false, ETabs.Menu);  //otherwise arrows do not refresh
        }

        private void manualToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //System.Windows.Forms.Help.ShowHelp(this, Application.StartupPath + "\\helpfiles\\gekko.chm", "i_overview.htm");
            Program.Help("i_overview.htm");
        }

        private void viewDatabanksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WindowOpenDatabanks wd = new WindowOpenDatabanks();
            wd.ShowDialog();
        }

        //private void runStatusToolStripMenuItem_Click(object sender, EventArgs e)
        //{
        //    OpenRunStatusWindow();
        //}

        private void toolStripStatusLabel3_DoubleClick(object sender, EventArgs e)
        {
            OpenRunStatusWindow();
        }

        private static void OpenRunStatusWindow()
        {
            if (Globals.windowRunStatus == null)
            {
                Globals.windowRunStatus = new WindowRunStatus();
                Globals.windowRunStatus.Show();
            }
            else
            {
                Globals.windowRunStatus.Focus();
            }
        }

        private void splitContainer1_Panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void runStatusToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            OpenRunStatusWindow();
        }

        private void allPPLOTUDVALGToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CloseAllDecompUdvalg();
        }

        public static void CloseAllDecompUdvalg()
        {
            //close all PPLOT+UDVALG
            List<Graph> windowsGraphTemp = new List<Graph>();
            windowsGraphTemp.AddRange(Globals.windowsGraph);
            Globals.ch = new CounterHelper();
            for (int i = 0; i < windowsGraphTemp.Count; i++)
            {
                CrossThreadStuff.CloseGraph(windowsGraphTemp[i]);  //fails silently
            }            
            List<Window1> windowsDecompTemp = new List<Window1>();
            windowsDecompTemp.AddRange(Globals.windowsDecomp);
            for (int i = 0; i < windowsDecompTemp.Count; i++)
            {
                CrossThreadStuff.CloseDecomp(windowsDecompTemp[i]);  //fails silently
            }            
            G.Writeln();
            G.Writeln("Closed " + Globals.ch.windowsGraphCloseCounter + " PLOT windows");
            G.Writeln("Closed " + Globals.ch.windowsDecompCloseCounter + " DECOMP windows");
        }

        private void allPPLOTToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //close all PPLOT
            List<Graph> windowsGraphTemp = new List<Graph>();
            windowsGraphTemp.AddRange(Globals.windowsGraph);
            Globals.ch = new CounterHelper();
            for (int i = 0; i < windowsGraphTemp.Count; i++)
            {
                CrossThreadStuff.CloseGraph(windowsGraphTemp[i]);  //fails silently
            }
            G.Writeln2("Closed " + Globals.ch.windowsGraphCloseCounter + " PLOT windows");
        }

        private void allUDVALGToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //close all UDVALG
            List<Window1> windowsDecompTemp = new List<Window1>();
            //windowsDecompTemp.AddRange(windowsDecompTemp); <------------- erroneous in 1.6.5
            windowsDecompTemp.AddRange(Globals.windowsDecomp);
            Globals.ch = new CounterHelper();
            for (int i = 0; i < windowsDecompTemp.Count; i++)
            {
                CrossThreadStuff.CloseDecomp(windowsDecompTemp[i]);  //fails silently
            }
            G.Writeln2("Closed " + Globals.ch.windowsDecompCloseCounter + " DECOMP windows");
        }

        private void allPPLOTAndUDVALGToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //update all PPLOT+UDVALG
            Globals.ch = new CounterHelper();
            foreach (Graph g in Globals.windowsGraph)
            {
                CrossThreadStuff.UpdateGraph(g);
            }
            foreach (Window1 w in Globals.windowsDecomp)
            {
                CrossThreadStuff.UpdateDecomp(w);
            }
            G.Writeln();
            GraphUpdatePrint();
            DecompUpdatePrint();
        }

        private void allPPLOTToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //update all PPLOT
            Globals.ch = new CounterHelper();
            foreach (Graph g in Globals.windowsGraph)
            {
                CrossThreadStuff.UpdateGraph(g);
            }
            G.Writeln();
            GraphUpdatePrint();
        }

        private void allUDVALGToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //update all UDVALG
            Globals.ch = new CounterHelper();
            foreach(Window1 w in Globals.windowsDecomp)
            {
                CrossThreadStuff.UpdateDecomp(w);
            }
            G.Writeln();
            DecompUpdatePrint();
        }

        private static void DecompUpdatePrint()
        {
            G.Writeln("Updated " + Globals.ch.windowsDecompUpdateCounter + " DECOMP windows");
            if (Globals.ch.windowsDecompUpdateFailedCounter > 0) G.Writeln("+++ WARNING: Failed updating " + Globals.ch.windowsDecompUpdateFailedCounter + " DECOMP windows");
        }

        private static void GraphUpdatePrint()
        {
            G.Writeln("Updated " + Globals.ch.windowsGraphUpdateCounter + " PLOT windows");
            if (Globals.ch.windowsGraphUpdateFailedCounter > 0) G.Writeln("+++ WARNING: Failed updating " + Globals.ch.windowsGraphUpdateFailedCounter + " PLOT windows");
        }

        private void cutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            SendKeys.SendWait("^x");
        }

        private void copyToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            SendKeys.SendWait("^c");
        }

        private void pasteToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            SendKeys.SendWait("^v");
        }

        private void undoToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            SendKeys.SendWait("^z");
        }

        private void redoToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            SendKeys.SendWait("^y");
        }

        private void selectAllToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            SendKeys.SendWait("^a");
        }

        private void commandHistoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<string> ss = G.ExtractLinesFromText(Globals.commandMemory.storage.ToString());
            List<string> ss2 = new List<string>();
            foreach (string s in ss)
            {
                if (s.Trim() == "") continue;
                if (s == Globals.iniFileSecretName) continue;
                ss2.Add(s);
            }
            WindowMessageBox w = new WindowMessageBox();
            w.textBox1.FontFamily = new System.Windows.Media.FontFamily("Courier New");
            w.textBox1.FontSize = 11;
            w.Title = "Command history since last clearing of workspace";
            w.textBox1.Text = G.ExtractTextFromLines(ss2).ToString();
            w.ShowDialog();
        }

        private void clearCommandHistoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Globals.commandMemory = new CommandMemory();
            G.Writeln2("Command memory cleared. (Note: this is done automatically");
            G.Writeln("when the workspace is cleared (RESET/RESTART)).");
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            Program.PrtClipboard(Globals.lastPrtOrMulprtTable, true);
        }

        private void deleteTempFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult myDialogResult = MessageBox.Show("Temporary files are Gekko-made temporary files, most notably " + G.NL + " cached model files in binary form. Clicking 'Yes' now will delete " + G.NL + " these files, saving some room on the hard disk, and forcing  " + G.NL + " Gekko to re-parse and re-compile a model, even if it has been  " + G.NL + " loaded before.", "Delete temp files", MessageBoxButtons.YesNo);
            if (myDialogResult == DialogResult.Yes)
            {
                Program.Flush();
            }
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            if (Gui.gui.tabControl1.SelectedIndex == 0)  //main
            {
                if (Program.guiBrowseHistory != null && Program.guiBrowseHistory.Count >= 1)
                {
                    string var = Program.guiBrowseHistory[0];
                    Program.guiBrowseNumber = 0;
                    Program.guiBrowseHistory.Clear();
                    if (var != "")
                    {
                        List<string> temp = new List<string>();
                        temp.Add(var);
                        Program.Disp(Globals.dispLastDispStart, Globals.dispLastDispEnd, temp, null);
                    }
                }

            }
            else if (Gui.gui.tabControl1.SelectedIndex == 2)  //menu
            {
                CrossThreadStuff.RestartMenuBrowser();
            }
        }

        private void showMenuToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CrossThreadStuff.SetTab("menu", true);
            CrossThreadStuff.RestartMenuBrowser();
        }
    }
}
