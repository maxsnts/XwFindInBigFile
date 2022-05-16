using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace XwFindInBigFile
{
    public partial class Main : Form
    {
        StringBuilder outputSB = null;
        string CurrentVersion = "";
        int barMax = 500;
        int TotalMatches = 0;

        //****************************************************************************************************
        public Main()
        {
            InitializeComponent();
            CurrentVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(
               System.Reflection.Assembly.GetAssembly(typeof(Main)).Location).FileVersion.ToString();
            Text = $"XwFindInBigFile v{CurrentVersion}";
        }

        //****************************************************************************************************
        private void Main_Load(object sender, EventArgs e)
        {
            progressBar1.Step = 1;
            progressBar1.Minimum = 0;
            progressBar1.Maximum = barMax;
            progressBar1.Value = 0;

            textBoxFile.Text = string.Empty;
            textBoxSearch.Text = string.Empty;

#if DEBUG
            textBoxFile.Text = @"\\Mac\Home\Desktop\feed-1.xml";
            textBoxSearch.Text = @"3 swimming pools";
#endif
        }

        //****************************************************************************************************
        private void button1_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            openFileDialog1.Multiselect = false;
            openFileDialog1.FileName = "";
            openFileDialog1.ShowDialog();
        }

        //****************************************************************************************************
        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            textBoxFile.Text = openFileDialog1.FileName;
        }

        //****************************************************************************************************
        private void buttonSearch_Click(object sender, EventArgs e)
        {
            if (backgroundWorker1.IsBusy)
            {
                backgroundWorker1.CancelAsync();
                buttonSearch.Text = "Canceling...";
                return;
            }

            if (textBoxSearch.Text.Trim() == string.Empty)
            {
                MessageBox.Show("Search string/regex can not be empty");
                return;
            }

            if (!File.Exists(textBoxFile.Text))
            {
                MessageBox.Show("Selected file not found");
                return;
            }

            progressBar1.Value = 0;
            textResult.Text = string.Empty;
            outputSB = new StringBuilder();
            labelMatches.Text = "- - -";
            TotalMatches = 0;

            backgroundWorker1.RunWorkerAsync();
            buttonSearch.Text = "Cancel";
        }
                
        //****************************************************************************************************
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            string logFilePath = textBoxFile.Text.Trim();
            string filter = textBoxSearch.Text;
            bool useRegex = checkBoxRegex.Checked;

            FileInfo info = new FileInfo(logFilePath);
            long FileSize = info.Length;
            long readPosition = 0;
            int LastPercent = 0;

            int QueueLines = (int)numericLines.Value;
            Queue<string> queueedLines = new Queue<string>(10);
            
            var lines = File.ReadLines(logFilePath);
            int index = 0;
            int ouputNextXLines = 0;
            foreach (string line in lines)
            {
                if ((backgroundWorker1.CancellationPending == true))
                {
                    e.Cancel = true;
                    return;
                }

                if (ouputNextXLines > 0)
                {
                    outputSB.Append(line + "\r\n");
                    ouputNextXLines--;
                }

                queueedLines.Enqueue(line);
                if (queueedLines.Count == (QueueLines*2) + 1)
                    queueedLines.Dequeue();

                index++;
                readPosition += line.Length;
                int Percent = (int)(readPosition * barMax / FileSize);
                if (Percent != LastPercent)
                {
                    backgroundWorker1.ReportProgress(Percent);
                    LastPercent = Percent;
                }
                
                bool found = false;
                if (useRegex)
                    found = Regex.Match(line, filter).Success;
                else
                    found = line.Contains(filter);

                if (found)
                {
                    TotalMatches++;
                    outputSB.Append($"\r\n################################################## Line: {index} ###########################################################\r\n");
                    foreach (string l in queueedLines)
                    {
                        outputSB.Append(l + "\r\n");
                    }
                    ouputNextXLines = QueueLines;
                }
            }

            backgroundWorker1.ReportProgress(barMax);
        }

        //****************************************************************************************************

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        //****************************************************************************************************
        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if ((e.Cancelled == true))
            {
                textResult.Text = "Canceled!";
                progressBar1.Value = 0;
            }
            else if (!(e.Error == null))
            {
                textResult.Text = ("Error: " + e.Error.Message);
            }

            else
            {
                textResult.Text = outputSB.ToString();
            }
            buttonSearch.Text = "Search";
            textResult.Show();
            labelMatches.Text = TotalMatches.ToString();
        }
    }
}
