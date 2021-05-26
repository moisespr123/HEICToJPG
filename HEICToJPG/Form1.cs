using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ImageMagick;

namespace HEICToJPG
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.CPUThreads == 0)
                CPUThreads.Value = Environment.ProcessorCount;
            else
                CPUThreads.Value = Properties.Settings.Default.CPUThreads;
            CPUThreads.Maximum = Environment.ProcessorCount;
        }

        private void convert(string input, string output)
        {
            using (MagickImage image = new MagickImage(input))
            {
                image.Write(output);
            }
            progressBar1.BeginInvoke(new MethodInvoker(() => progressBar1.PerformStep()));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.ShowNewFolderButton = false;
            folderBrowserDialog.ShowDialog();
            inputTextbox.Text = folderBrowserDialog.SelectedPath;
        }

        private void browseOutput_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.ShowNewFolderButton = true;
            folderBrowserDialog.ShowDialog();
            outputTextBox.Text = folderBrowserDialog.SelectedPath;
        }

        private void ConvertButton_Click(object sender, EventArgs e)
        {

            if (!string.IsNullOrEmpty(outputTextBox.Text) && !string.IsNullOrEmpty(inputTextbox.Text))
            {
                inputTextbox.Enabled = false;
                outputTextBox.Enabled = false;
                browseInput.Enabled = false;
                browseOutput.Enabled = false;
                ConvertButton.Enabled = false;
                CPUThreads.Enabled = false;
                if (!Directory.Exists(outputTextBox.Text)) Directory.CreateDirectory(outputTextBox.Text);
                System.Threading.Thread thread = new System.Threading.Thread(() => threadCode());
                thread.Start();
            }
            else
            {
                if (string.IsNullOrEmpty(inputTextbox.Text))
                {
                    MessageBox.Show("The Input Directory cannot be empty.");
                    return;
                }
                if (string.IsNullOrEmpty(outputTextBox.Text))
                {
                    MessageBox.Show("The Output Directory cannot be empty.");
                    return;
                }
            }
        }
        private void threadCode()
        {
            List<string> ItemsToProcess = new List<string>();

            if (Directory.Exists(inputTextbox.Text))
            {
                foreach (string file in Directory.GetFiles(inputTextbox.Text))
                {
                    if (Path.GetExtension(file) == ".heic")
                        ItemsToProcess.Add(file);
                }

                progressBar1.BeginInvoke(new MethodInvoker(() =>
                {
                    progressBar1.Maximum = ItemsToProcess.Count();
                    progressBar1.Value = 0;
                }));

                List<Action> tasks = new List<Action>();
                for (int i = 0; i <= ItemsToProcess.Count() - 1; i++)
                {
                    string inputPath = ItemsToProcess[i];
                    string OutputPath = outputTextBox.Text + @"\" + Path.GetFileNameWithoutExtension(ItemsToProcess[i]) + ".jpg";
                    if (!File.Exists(OutputPath))
                    {
                        tasks.Add(() => convert(inputPath, OutputPath));
                    }
                }
                Parallel.Invoke(new ParallelOptions() { MaxDegreeOfParallelism = Properties.Settings.Default.CPUThreads }, tasks.ToArray());
                ConvertButton.BeginInvoke(new MethodInvoker(() =>
                {
                    ConvertButton.Enabled = true;
                    inputTextbox.Enabled = true;
                    outputTextBox.Enabled = true;
                    browseInput.Enabled = true;
                    browseOutput.Enabled = true;
                    CPUThreads.Enabled = true;
                }));
                MessageBox.Show("Conversion finished!");
            }
        }


        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            if (ConvertButton.Enabled)
                inputTextbox.Text = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
        }

        private void CPUThreads_ValueChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.CPUThreads = (int)CPUThreads.Value;
            Properties.Settings.Default.Save();
        }
    }
}
