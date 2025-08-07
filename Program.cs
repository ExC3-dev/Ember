// Ember Notepad Clone with enhanced features and stability improvements
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Ember
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new NotepadForm());
        }
    }

    public class NotepadForm : Form
    {
        private ToolStrip toolBar;
        private ToolStripButton openBtn, saveBtn, saveAsBtn, compileBtn, createModBtn;
        private SplitContainer splitContainer;
        private RichTextBox editor;
        private RichTextBox output;
        private OpenFileDialog openFile;
        private SaveFileDialog saveFile;
        private string currentFilePath = null;
        private string currentProjPath = null;
        private PictureBox lineNumbers;
        private bool isDragging = false;
        private int minSplitter = 100;
        private int maxSplitter => this.ClientSize.Height - 100;

        public NotepadForm()
        {
            this.Text = "Ember";
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.FromArgb(30, 30, 30);
            try
            {
                this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load icon: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            toolBar = new ToolStrip { BackColor = Color.FromArgb(45, 45, 45), ForeColor = Color.White };
            openBtn = new ToolStripButton("Open");
            saveBtn = new ToolStripButton("Save");
            saveAsBtn = new ToolStripButton("Save As");
            compileBtn = new ToolStripButton("Compile");
            createModBtn = new ToolStripButton("Create Mod");
            toolBar.Items.AddRange(new ToolStripItem[] { openBtn, saveBtn, saveAsBtn, compileBtn, createModBtn });
            this.Controls.Add(toolBar);

            splitContainer = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
            splitContainer.SplitterDistance = this.ClientSize.Height * 3 / 4;
            splitContainer.SplitterWidth = 8;
            splitContainer.Panel1.BackColor = Color.FromArgb(30, 30, 30);
            splitContainer.Panel2.BackColor = Color.FromArgb(30, 30, 30);
            splitContainer.MouseDown += (s, e) => isDragging = true;
            splitContainer.MouseUp += (s, e) => isDragging = false;
            splitContainer.MouseMove += (s, e) =>
            {
                if (e.Y > splitContainer.SplitterDistance - 4 && e.Y < splitContainer.SplitterDistance + 4)
                    Cursor = Cursors.HSplit;
                else
                    Cursor = Cursors.Default;

                if (isDragging)
                {
                    int newDistance = Math.Max(minSplitter, Math.Min(e.Y, maxSplitter));
                    splitContainer.SplitterDistance = newDistance;
                }
            };
            this.Controls.Add(splitContainer);
            splitContainer.BringToFront();

            lineNumbers = new PictureBox { Dock = DockStyle.Left, Width = 50, BackColor = Color.FromArgb(25, 25, 25) };
            lineNumbers.Paint += LineNumbers_Paint;

            editor = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Courier New", 14, FontStyle.Regular),
                BackColor = Color.FromArgb(20, 20, 20),
                ForeColor = Color.White,
                AcceptsTab = true,
                HideSelection = false,
                WordWrap = false,
                DetectUrls = false
            };
            editor.VScroll += (s, e) => lineNumbers.Invalidate();
            editor.TextChanged += Editor_TextChanged;
            editor.SelectionChanged += (s, e) => lineNumbers.Invalidate();
            editor.KeyDown += (s, e) => { if (e.Control && e.KeyCode == Keys.S) SaveBtn_Click(null, null); };
            editor.KeyPress += (s, e) => e.KeyChar = e.KeyChar;

            var editorPanel = new Panel { Dock = DockStyle.Fill };
            editorPanel.Controls.Add(editor);
            editorPanel.Controls.Add(lineNumbers);
            splitContainer.Panel1.Controls.Add(editorPanel);

            output = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Courier New", 12, FontStyle.Regular),
                BackColor = Color.FromArgb(20, 20, 20),
                ForeColor = Color.LightGray,
                ReadOnly = true,
                DetectUrls = false
            };
            splitContainer.Panel2.Controls.Add(output);

            openFile = new OpenFileDialog { Filter = "C# Files|*.cs|All Files|*.*" };
            saveFile = new SaveFileDialog { Filter = "C# Files|*.cs|All Files|*.*" };

            openBtn.Click += OpenBtn_Click;
            saveBtn.Click += SaveBtn_Click;
            saveAsBtn.Click += SaveAsBtn_Click;
            compileBtn.Click += CompileBtn_Click;
            createModBtn.Click += CreateModBtn_Click;
        }

        private void LineNumbers_Paint(object sender, PaintEventArgs e)
        {
            int firstLine = editor.GetCharIndexFromPosition(new Point(0, 0));
            int firstLineNumber = editor.GetLineFromCharIndex(firstLine);
            int lineHeight = TextRenderer.MeasureText("A", editor.Font).Height;
            int linesVisible = editor.Height / lineHeight;
            e.Graphics.Clear(Color.FromArgb(25, 25, 25));
            for (int i = 0; i <= linesVisible; i++)
            {
                int lineNum = firstLineNumber + i + 1;
                int y = i * lineHeight - 2;
                e.Graphics.DrawString(lineNum.ToString(), editor.Font, Brushes.Gray, 0, y);
            }
        }

        private void AppendOutput(string text, Color color)
        {
            output.SelectionStart = output.TextLength;
            output.SelectionLength = 0;
            output.SelectionColor = color;
            output.AppendText(text + Environment.NewLine);
            output.SelectionColor = output.ForeColor;
        }

        private void OpenBtn_Click(object sender, EventArgs e)
        {
            if (openFile.ShowDialog() == DialogResult.OK)
            {
                currentFilePath = openFile.FileName;
                editor.Text = File.ReadAllText(currentFilePath);
                editor.Select(0, 0);
                HighlightSyntax();
            }
        }

        private void SaveBtn_Click(object sender, EventArgs e)
        {
            if (currentFilePath != null)
            {
                File.WriteAllText(currentFilePath, editor.Text);
                AppendOutput($"Saved to {currentFilePath}", Color.Yellow);
            }
            else
            {
                SaveAsBtn_Click(sender, e);
            }
        }

        private void SaveAsBtn_Click(object sender, EventArgs e)
        {
            if (saveFile.ShowDialog() == DialogResult.OK)
            {
                currentFilePath = saveFile.FileName;
                File.WriteAllText(currentFilePath, editor.Text);
                AppendOutput($"Saved to {currentFilePath}", Color.Yellow);
            }
        }

        private void CompileBtn_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                AppendOutput("Save the file before compiling.", Color.Red);
                return;
            }

            string dir = Path.GetDirectoryName(currentFilePath);

            if (string.IsNullOrEmpty(currentProjPath))
            {
                currentProjPath = Path.Combine(dir, "EmberTempProj.csproj");
                string projContent =
                    "<Project Sdk=\"Microsoft.NET.Sdk\">\n" +
                    "  <PropertyGroup>\n" +
                    "    <TargetFramework>net6.0</TargetFramework>\n" +
                    "    <OutputType>Library</OutputType>\n" +
                    "  </PropertyGroup>\n" +
                    "  <ItemGroup>\n" +
                    $"    <Compile Include=\"{Path.GetFileName(currentFilePath)}\" />\n" +
                    "  </ItemGroup>\n" +
                    "</Project>";
                File.WriteAllText(currentProjPath, projContent);
            }

            ProcessStartInfo psi = new ProcessStartInfo("dotnet", $"build \"{currentProjPath}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = dir,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process p = new Process { StartInfo = psi };
            p.Start();
            string result = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
            p.WaitForExit();

            Color resultColor = result.Contains("error") ? Color.Red : Color.LimeGreen;
            AppendOutput("--- Compile Output ---", Color.Yellow);
            AppendOutput(result.Trim(), resultColor);

            string dllPath = Path.Combine(dir, "bin", "Debug", "net6.0", Path.GetFileNameWithoutExtension(currentFilePath) + ".dll");
            string destPath = Path.Combine(dir, Path.GetFileNameWithoutExtension(currentFilePath) + ".dll");
            if (File.Exists(dllPath))
            {
                File.Copy(dllPath, destPath, true);
                AppendOutput($"DLL copied to {destPath}", Color.Yellow);
            }
        }

        private void CreateModBtn_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    var dir = dlg.SelectedPath;
                    var csPath = Path.Combine(dir, "ModTemplate.cs");
                    File.WriteAllText(csPath, "using System;\nnamespace MyMod { public class ModTemplate { public void Init() { /* TODO */ } } }", Encoding.UTF8);
                    var projPath = Path.Combine(dir, "MyMod.csproj");
                    string projContent =
                        "<Project Sdk=\"Microsoft.NET.Sdk\">\n" +
                        "  <PropertyGroup>\n" +
                        "    <TargetFramework>net6.0</TargetFramework>\n" +
                        "    <OutputType>Library</OutputType>\n" +
                        "  </PropertyGroup>\n" +
                        "</Project>";
                    File.WriteAllText(projPath, projContent, Encoding.UTF8);
                    AppendOutput($"Mod template created in {dir}", Color.Yellow);
                }
            }
        }

        private void Editor_TextChanged(object sender, EventArgs e) => HighlightSyntax();

        private void HighlightSyntax()
        {
            int selStart = editor.SelectionStart;
            int selLen = editor.SelectionLength;
            editor.SelectAll();
            editor.SelectionColor = Color.White;

            var rules = new[]
            {
                ("\\b(class|namespace|using|public|private|protected|static|void|return|if|else|for|foreach|while|break|continue|new|try|catch|finally|switch|case|default|this|base)\\b", Color.Cyan),
                ("\\b(int|float|double|bool|string|char|long|decimal|var|object)\\b", Color.Yellow),
                ("\\b\\w+(?=\\s*\\()", Color.MediumPurple),
                ("\".*?\"", Color.LightGreen),
                ("//.*", Color.Gray),
                ("#.*", Color.DarkGray),
                ("\\b(Error|Exception|NullReference|IndexOutOfRange)\\b", Color.Red)
            };

            foreach (var (pattern, color) in rules)
            {
                foreach (Match m in Regex.Matches(editor.Text, pattern))
                {
                    editor.Select(m.Index, m.Length);
                    editor.SelectionColor = color;
                }
            }

            editor.Select(selStart, selLen);
        }
    }
}