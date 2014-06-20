using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AScript
{
    public class AEngine
    {
        private string source;

        public string Source
        {
            get { return source; }
            set { source = value; }
        }

        private struct Argument
        {
            public string Type;
            public string Value;
        };

        private struct FunctionType
        {
            public string Type;
            public string Name;
        };

        public AEngine()
        {

        }

        public void Run(Action<string> Write)
        {
            if (Write == null)
                return;

            string output = "";

            Source = Source.Replace(((char)13).ToString(), "");

            if(Source == String.Empty || Source == null)
            {
                ErrorMsg("Source can not be empty!\r\nEnding parsing!");
                return;
            }

            int numOfLines = 0;

            for (int i = 0; i < Source.Length; i++)
            {
                if(source[i] == '\n')
                {
                    numOfLines++;
                }
            }

            string[] line = new string[numOfLines];
            string cline = "";
            int ccount = 0;

            for (int i = 0; i < Source.Length; i++)
            {
                if (source[i] == '\n')
                {
                    line[ccount] = cline;
                    cline = "";
                    ccount++;
                    continue;
                }

                cline += source[i];
            }

            
            /*
             * Actual Script Parsing!
             */
            Dictionary<string, int> ints = new Dictionary<string, int>();
            Dictionary<string, string> strings = new Dictionary<string, string>();
            Dictionary<string, bool> bools = new Dictionary<string, bool>();
            Dictionary<FunctionType, Argument[]> functions = new Dictionary<FunctionType, Argument[]>();

            bool breakOut = false;
            bool inString = false;
            bool inFuncDecl = false;
            string varName = "";
            string varValue = "";
            List<Argument> funcArgs = new List<Argument>();

            for (int i = 0; i < numOfLines; i++)
            {
                if (breakOut)
                    break;

                string currLine = line[i];

                if (!inString)
                {
                    if (currLine == String.Empty)
                        continue;

                    if (currLine[0] == '\t')
                        currLine.TrimStart('\t');

                    if (currLine.StartsWith("#"))
                    {
                        continue;
                    }

                    if (currLine.StartsWith("string "))
                    {
                        string[] s = currLine.Split(' ');

                        inString = true;

                        if (s.Count() < 3)
                        {
                            ErrorMsg(String.Format("Not enough parameters to declare string object!\nLine: {0}", i + 1));
                            breakOut = true;
                            break;
                        }

                        if (IsReservedKeyword(s[1]))
                        {
                            ErrorMsg(String.Format("Invalid string name!\n'{0}'\nLine: {1}\n", s[1], i + 1));
                            breakOut = true;
                            break;
                        }

                        varName = s[1];

                        if (s[2] != "=")
                        {
                            ErrorMsg(String.Format("Missing assign operator '='!\nLine: {0}", i + 1));
                            breakOut = true;
                            break;
                        }

                        if (!s[3].StartsWith("\""))
                        {
                            ErrorMsg(String.Format("Expecting \" after '=' operator!\nLine: {0}", i + 1));
                            breakOut = true;
                            break;
                        }

                        if (!s[s.Count() - 1].EndsWith("\";"))
                        {
                            bool endFound = false;
                            // Try find end of string
                            for (int k = i; k < line.Count(); k++)
                            {
                                if(line[k].Contains("\";"))
                                {
                                    endFound = true;
                                }
                            }

                            if (!endFound)
                            {
                                ErrorMsg(String.Format("Missing valid end of string assigning!\nLine: {0}", i + 1));
                                breakOut = true;
                                break;
                            }
                        }

                        for (int k = 3; k < s.Count(); k++)
                        {
                            if (s[k].EndsWith("\";") && k != 3)
                            {
                                varValue += s[k];
                                varValue = varValue.Remove(varValue.Length - 2, 2);
                                inString = false;
                                strings.Add(varName, varValue);
                                varName = "";
                                varValue = "";
                                break;
                            }
                            else if (s[k].EndsWith("\";") && k == 3)
                            {
                                varValue += s[k];
                                varValue = varValue.Remove(0, 1);
                                varValue = varValue.Remove(varValue.Length - 2, 2);
                                inString = false;
                                strings.Add(varName, varValue);
                                varName = "";
                                varValue = "";
                                break;
                            }

                            varValue += s[k] + " ";

                            if (k == 3)
                            {
                                varValue = varValue.Remove(0, 1);
                            }
                        }
                    }
                    else if(currLine.StartsWith("void "))
                    {
                        string[] s = currLine.Split(' ');

                        if (s.Count() < 2)
                        {
                            ErrorMsg(String.Format("Not enough parameters to declare a function member!\nLine: {0}", i + 1));
                            breakOut = true;
                            break;
                        }

                        if(s.Count() == 2)
                        {
                            if(!s[1].EndsWith("()"))
                            {
                                ErrorMsg(String.Format("Function does not end correctly!\nMissing () ?\nLine: {0}", i + 1));
                                breakOut = true;
                                break;
                            }
                            else
                            {
                                string sn = "";
                                string name = s[1];

                                for(int k = 0; k < name.Length; k++)
                                {
                                    if(name[k] == '(')
                                    {
                                        Argument[] a = new Argument[0];

                                        FunctionType type = new FunctionType();
                                        type.Type = "VOID";
                                        type.Name = sn;

                                        functions.Add(type, a);
                                        break;
                                    }

                                    sn += name[k];
                                }
                            }
                        }
                        else
                        {
                            bool bFound = false;
                            bool isArgAfterParant = false;
                            int parantIndex = 0;
                            string chk = s[1];

                            for (int k = 0; k < s[1].Length; k++)
                            {
                                if(chk[k] == '(')
                                {
                                    parantIndex = k + 1;
                                    bFound = true;
                                }

                                if(bFound)
                                {
                                    if(k < s[1].Length - 1)
                                    {
                                        if(chk[k + 1] != ' ')
                                        {
                                            isArgAfterParant = true;
                                        }
                                    }
                                }
                            }

                            if(!bFound)
                            {
                                ErrorMsg(String.Format("Function error!\nUse the following function pattern:\ntype functionName()\nLine: {0}", i + 1));
                                breakOut = true;
                                break;
                            }

                            bool typeSet = false;
                            Argument tmpArg = new Argument();

                            for (int v = 1; v < s.Count(); v++)
                            {
                                string tmpStr = s[v];

                                if (tmpStr == ",")
                                    continue;

                                if (v == 1 && !isArgAfterParant)
                                    continue;

                                if(isArgAfterParant)
                                {
                                    string varType = "";

                                    for (int y = parantIndex; y < s[v].Length; y++ )
                                    {
                                        varType += tmpStr[y];
                                    }

                                    tmpArg.Type = varType.ToUpper();
                                    typeSet = true;
                                    isArgAfterParant = false;
                                }
                                else
                                {
                                    if(typeSet)
                                    {
                                        if(IsReservedKeyword(s[v]))
                                        {
                                            ErrorMsg(String.Format("Invalid " + tmpArg.Type + " name!\n\nLine: {0}\n", i + 1));
                                            breakOut = true;
                                            break;
                                        }

                                        if (s[v].StartsWith(","))
                                        {
                                            s[v] = s[v].Remove(0, 1);
                                        }

                                        if (s[v].EndsWith(","))
                                        {
                                            s[v] = s[v].Remove(s[v].Length - 1, 1);
                                        }

                                        if(s[v].EndsWith(")"))
                                        {
                                            s[v] = s[v].Remove(s[v].Length - 1, 1);
                                        }

                                        if(ContainsInvalidChar(s[v]))
                                        {
                                            ErrorMsg(String.Format("Invalid character in variable name!\n\nLine: {0}\n", i + 1));
                                            breakOut = true;
                                            break;
                                        }

                                        tmpArg.Value = s[v];
                                        typeSet = false;
                                        funcArgs.Add(tmpArg);
                                        tmpArg = new Argument();
                                    }
                                    else if(!typeSet)
                                    {
                                        if(s[v].StartsWith(","))
                                        {
                                            s[v] = s[v].Remove(0, 1);
                                        }

                                        tmpArg.Type = s[v].ToUpper();
                                        typeSet = true;
                                    }
                                }
                            }


                            string sn = "";
                            string name = s[1];

                            for (int k = 0; k < name.Length; k++)
                            {
                                if (name[k] == '(')
                                {
                                    FunctionType type = new FunctionType();
                                    type.Type = "VOID";
                                    type.Name = sn;

                                    functions.Add(type, funcArgs.ToArray());
                                    funcArgs.Clear();
                                    break;
                                }

                                sn += name[k];
                            }
                        }
                    }
                }
                else if(inString)
                {
                    string[] s = currLine.Split(' ');
                    bool didEnd = false;

                    for (int k = 0; k < s.Count(); k++)
                    {
                        if (s[k].EndsWith("\";"))
                        {
                            varValue += s[k];
                            varValue = varValue.Remove(varValue.Length - 2, 2);
                            inString = false;
                            didEnd = true;
                            break;
                        }

                        varValue += s[k] + " ";
                    }

                    if (didEnd)
                    {
                        strings.Add(varName, varValue);
                        varName = "";
                        varValue = "";
                        inString = false;
                    }
                }
            }

            foreach(KeyValuePair<string, string> i in strings)
            {
                Write(i.Key + " = " + i.Value + "\n");
            }

            foreach(KeyValuePair<FunctionType, Argument[]> func in functions)
            {
                Write("Function: (" + func.Key.Type + ")" + func.Key.Name + " has " + func.Value.Count().ToString() + " argument(s)  ->  \nArgs: [");

                for(int i = 0; i < func.Value.Count(); i++)
                {
                    if(i == func.Value.Count() - 1)
                        Write("(" + func.Value[i].Type + ")" + func.Value[i].Value);
                    else
                        Write("(" + func.Value[i].Type + ")" + func.Value[i].Value + ", ");
                }
                Write("]\n");
            }
        }

        private string[] TrimArray(string[] arr)
        {
            foreach (string s in arr)
                s.Trim();

            return arr;
        }

        private bool ContainsInvalidChar(string varName)
        {
            if (varName.Contains('!') || varName.Contains('.') || varName.Contains('(') || varName.Contains(')'))
                return true;

            return false;
        }

        private bool IsReservedKeyword(string name)
        {
            if (name == "string" || name == "int" || name == "bool" || name == "void")
                return true;
            else
                return false;
        }

        private void ErrorMsg(string msg)
        {
            MessageBox.Show(msg, "AScript Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
