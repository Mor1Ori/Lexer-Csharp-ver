using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;

namespace Lexer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void SelectFileButton_Click(object sender, EventArgs e)
        {
            // 创建并配置OpenFileDialog
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*";
                openFileDialog.Title = "选择一个文本文件";

                // 显示对话框并检查用户是否选择了文件
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // 读取文件内容
                        string fileContent = File.ReadAllText(openFileDialog.FileName);

                        // 将内容显示在TextBox中
                        textBox1.Text = fileContent;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"读取文件时发生错误: {ex.Message}", "错误",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void AnalyzeButton_Click(object sender, EventArgs e)
        {
            string source = textBox1.Text;
            if (string.IsNullOrEmpty(source))
            {
                MessageBox.Show("请先加载源程序！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // 执行词法分析
                var lexer = new Lexer(source);
                var result = lexer.Analyze();


                // 显示结果到文本框2
                textBox2.Text = result.Output;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"词法分析时发生错误: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    public class Lexer
    {
        private readonly string source;
        private int pos;
        private readonly List<string> identifiers = new List<string>(); // 标识符表
        private readonly List<int> constants = new List<int>();        // 常数表
        private readonly List<string> errors = new List<string>();     // 错误信息
        private readonly List<string> tokenLines = new List<string>(); // 单词串
        private readonly HashSet<string> keywords = new HashSet<string> { "if", "else" };
        private readonly HashSet<string> operators = new HashSet<string> { "+", "-", "*", "/", ":=", "=", "<>", "<", ">", "<=", ">=" };
        private readonly HashSet<char> delimiters = new HashSet<char> { ';', '(', ')', '#' };

        public Lexer(string source)
        {
            this.source = source;
            pos = 0;
        }

        public (string Output, List<string> TokenLines) Analyze()
        {
            while (pos < source.Length)
            {
                char c = source[pos];

                if (char.IsWhiteSpace(c))
                {
                    pos++;
                    continue;
                }

                if (char.IsLetter(c))
                {
                    ScanIdentifierOrKeyword();
                }
                else if (char.IsDigit(c))
                {
                    ScanNumber();
                }
                else if (delimiters.Contains(c))
                {
                    tokenLines.Add($"({GetTokenType(c.ToString())}, -)");
                    pos++;
                }
                else if (c == '+' || c == '-' || c == '*' || c == '/' || c == '=' || c == '<' || c == '>')
                {
                    ScanOperator();
                }
                else if (c == ':')
                {
                    ScanAssign();
                }
                else
                {
                    errors.Add($"错误: 位置 {pos + 1} 处的字符 '{c}' 未定义");
                    pos++;
                }
            }

            // 检查是否以#结束
            if (tokenLines.Count == 0 || tokenLines[tokenLines.Count - 1] != "(#, -)")
            {
                errors.Add("错误: 源程序未以'#'结束");
            }

            // 构建输出
            StringBuilder output = new StringBuilder();
            output.AppendLine("=== 词法分析结果 ===");
            output.AppendLine("\n标识符表:");
            for (int i = 0; i < identifiers.Count; i++)
            {
                output.AppendLine($"{i + 1}: {identifiers[i]}");
            }
            output.AppendLine("\n常数表:");
            for (int i = 0; i < constants.Count; i++)
            {
                output.AppendLine($"{i + 1}: {constants[i]}");
            }
            output.AppendLine("\n单词串:");
            foreach (var token in tokenLines)
            {
                output.AppendLine(token);
            }
            if (errors.Count > 0)
            {
                output.AppendLine("\n词法错误:");
                foreach (var error in errors)
                {
                    output.AppendLine(error);
                }
            }

            return (output.ToString(), tokenLines);
        }

        private void ScanIdentifierOrKeyword()
        {
            StringBuilder sb = new StringBuilder();
            while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_'))
            {
                sb.Append(source[pos]);
                pos++;
            }
            string token = sb.ToString();
            if (keywords.Contains(token))
            {
                tokenLines.Add($"({token}, -)");
            }
            else
            {
                int index = identifiers.IndexOf(token);
                if (index == -1)
                {
                    identifiers.Add(token);
                    index = identifiers.Count - 1;
                }
                tokenLines.Add($"(id, {index + 1})");
            }
        }

        private void ScanNumber()
        {
            StringBuilder sb = new StringBuilder();
            while (pos < source.Length && char.IsDigit(source[pos]))
            {
                sb.Append(source[pos]);
                pos++;
            }
            if (int.TryParse(sb.ToString(), out int value))
            {
                int index = constants.IndexOf(value);
                if (index == -1)
                {
                    constants.Add(value);
                    index = constants.Count - 1;
                }
                tokenLines.Add($"(num, {index + 1})");
            }
            else
            {
                errors.Add($"错误: 位置 {pos - sb.Length + 1} 处的数字 '{sb}' 无效");
            }
        }

        private void ScanOperator()
        {
            string op = source[pos].ToString();
            pos++;
            if (pos < source.Length && (op == "<" || op == ">") && source[pos] == '=')
            {
                op += "=";
                pos++;
            }
            else if (pos < source.Length && op == "<" && source[pos] == '>')
            {
                op += ">";
                pos++;
            }
            if (operators.Contains(op))
            {
                tokenLines.Add($"({op}, -)");
            }
            else
            {
                errors.Add($"错误: 位置 {pos - op.Length + 1} 处的运算符 '{op}' 无效");
            }
        }

        private void ScanAssign()
        {
            pos++;
            if (pos < source.Length && source[pos] == '=')
            {
                pos++;
                tokenLines.Add($"(:=, -)");
            }
            else
            {
                errors.Add($"错误: 位置 {pos} 处缺少 '='，无法形成赋值运算符 ':='");
            }
        }

        private string GetTokenType(string token)
        {
            if (keywords.Contains(token)) return token;
            if (operators.Contains(token)) return token;
            if (delimiters.Contains(token[0])) return token;
            return "unknown";
        }
    }

}
