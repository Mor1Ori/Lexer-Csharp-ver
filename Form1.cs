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
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*";
                openFileDialog.Title = "选择一个文本文件";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string fileContent = File.ReadAllText(openFileDialog.FileName);
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
        private readonly List<double> constants = new List<double>();   // 常数表
        private readonly List<string> errors = new List<string>();      // 错误信息
        private readonly List<string> tokenLines = new List<string>();  // 单词串
        private readonly HashSet<string> keywords = new HashSet<string> { "if", "else" };
        private readonly HashSet<string> operators = new HashSet<string> { "+", "-", "*", "/", "=", "==", "<>", "<", ">", "<=", ">=" };
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

                if (char.IsLetter(c) || char.IsDigit(c))
                {
                    // 尝试解析标识符或数字
                    int startPos = pos;
                    StringBuilder sb = new StringBuilder();
                    bool hasLetter = char.IsLetter(c);

                    // 读取字母、数字或下划线
                    while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_'))
                    {
                        sb.Append(source[pos]);
                        if (char.IsLetter(source[pos]))
                            hasLetter = true;
                        pos++;
                    }

                    string token = sb.ToString();

                    // 如果包含字母，尝试作为标识符或关键字
                    if (hasLetter)
                    {
                        if (keywords.Contains(token))
                        {
                            tokenLines.Add($"({token}, -)");
                        }
                        else
                        {
                            if (!char.IsLetter(token[0]))
                            {
                                errors.Add($"错误: 标识符 '{token}' 非法，必须以字母开头");
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
                    }
                    else
                    {
                        // 纯数字，回退并解析为数字
                        pos = startPos;
                        ScanNumber();
                    }
                }
                else if (c == '.')
                {
                    // 检查是否为浮点数
                    int startPos = pos;
                    StringBuilder sb = new StringBuilder();
                    sb.Append(c);
                    pos++;

                    // 读取小数部分
                    bool hasDigits = false;
                    while (pos < source.Length && char.IsDigit(source[pos]))
                    {
                        sb.Append(source[pos]);
                        hasDigits = true;
                        pos++;
                    }

                    string token = sb.ToString();

                    // 检查前一个字符是否为数字（整数部分）
                    bool hasIntegerPart = startPos > 0 && char.IsDigit(source[startPos - 1]);
                    if (!hasIntegerPart && !hasDigits)
                    {
                        errors.Add($"错误: token '{token}' 未定义");
                    }
                    else
                    {
                        // 回退并调用ScanNumber
                        pos = startPos;
                        ScanNumber();
                    }
                }
                else if (delimiters.Contains(c))
                {
                    tokenLines.Add($"({c}, -)");
                    pos++;
                }
                else if (c == '+' || c == '-' || c == '*' || c == '/' || c == '=' || c == '<' || c == '>')
                {
                    ScanOperator();
                }
                else
                {
                    errors.Add($"错误: token '{c}' 未定义");
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

        private void ScanNumber()
        {
            StringBuilder sb = new StringBuilder();
            bool hasDecimalPoint = false;
            int decimalPointCount = 0;
            bool hasDigits = false;
            bool hasIntegerPart = false;

            // 读取整数部分
            while (pos < source.Length && char.IsDigit(source[pos]))
            {
                sb.Append(source[pos]);
                hasDigits = true;
                hasIntegerPart = true;
                pos++;
            }

            // 读取小数点及小数部分
            if (pos < source.Length && source[pos] == '.')
            {
                hasDecimalPoint = true;
                decimalPointCount++;
                sb.Append(source[pos]);
                pos++;
                while (pos < source.Length && char.IsDigit(source[pos]))
                {
                    sb.Append(source[pos]);
                    hasDigits = true;
                    pos++;
                }
            }

            // 检查后续小数点（针对3.0.1）
            if (pos < source.Length && source[pos] == '.')
            {
                decimalPointCount++;
                sb.Append(source[pos]);
                pos++;
                while (pos < source.Length && char.IsDigit(source[pos]))
                {
                    sb.Append(source[pos]);
                    hasDigits = true;
                    pos++;
                }
            }

            string token = sb.ToString();

            // 检查数字合法性
            if (decimalPointCount > 1)
            {
                errors.Add($"错误: 数字 '{token}' 非法，包含多个小数点");
                return;
            }

            if (!hasDigits)
            {
                errors.Add($"错误: 数字 '{token}' 非法，缺少数字");
                return;
            }

            if (!hasDecimalPoint && token.Length > 1 && token[0] == '0')
            {
                errors.Add($"错误: 数字 '{token}' 非法，整数不能以0开头");
                return;
            }

            if (hasDecimalPoint && !hasIntegerPart)
            {
                errors.Add($"错误: 数字 '{token}' 非法，缺少整数部分");
                return;
            }

            if (double.TryParse(token, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double value))
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
                errors.Add($"错误: 数字 '{token}' 无效");
            }
        }

        private void ScanOperator()
        {
            string op = source[pos].ToString();
            pos++;

            // 检查多字符运算符
            if (pos < source.Length)
            {
                if (op == "=" && source[pos] == '=')
                {
                    op = "==";
                    pos++;
                }
                else if (op == "<" && source[pos] == '>')
                {
                    op = "<>";
                    pos++;
                }
                else if ((op == "<" || op == ">") && source[pos] == '=')
                {
                    op += "=";
                    pos++;
                }
            }

            if (operators.Contains(op))
            {
                tokenLines.Add($"({op}, -)");
            }
            else
            {
                errors.Add($"错误: token '{op}' 未定义");
            }
        }
    }
}

