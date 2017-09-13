using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace RC_SQL_TOOL
{
    public class SqlRow : ISqlRow
    {
        public string BaseTarget { get; set; }
        public string TableName { get; set; }
        public List<SqlRow_Union> PairCollection { get => _pairs; set => _pairs = value; }

        List<SqlRow_Union> _pairs = new List<SqlRow_Union>();

        public class SqlRow_Union
        {
            public string Key { get; set; }
            public string Value { get; set; }
            public override string ToString()
            {
                return Key + " " + Value;
            }
        }

        public override string ToString()
        {
            string res = BaseTarget + "." + TableName;
            foreach (var t in _pairs)
                res += "{" + t + "}";
            return res;
        }

        public string SqlRollbackDelete()
        {
            return "delete " + BaseTarget + "." + TableName + " t where t." + _pairs[0].Key + " in (" + _pairs[0].Value + ");";
        }

        public string SqlInsert()
        {
            if (_pairs.Count < 1) throw new Exception("sql syntax error");
            string res = "insert into " + BaseTarget + "." + TableName + " (";
            string tempres = "";
            foreach (var t in _pairs)
            {
                tempres += ",";
                tempres += t.Key;
            }
            res += tempres.Substring(1);
            res += ") \n values (";
            tempres = "";
            foreach (var t in _pairs)
            {
                tempres += ",";
                tempres += t.Value;
            }
            res += tempres.Substring(1);
            res += ");";
            return res;
        }

        public string SqlResetUpdate()
        {
            if (_pairs.Count < 2) throw new Exception("sql syntax error");
            string res = "update " + BaseTarget + "." + TableName + " set ";
            string tempstr = "";
            for (int i = 1; i < _pairs.Count; i++)
            {
                if (_pairs[i].Value == "''" || _pairs[i].Value == "null" || _pairs[i].Key.Contains("date") || _pairs[i].Value.ToLower() == "sysdate") continue;
                tempstr += "," + _pairs[i].Key + " = " + _pairs[i].Value;
            }

            res += tempstr.Substring(1);
            res += " where " + _pairs[0].Key + " = " + _pairs[0].Key + ";";

            return res;
        }

        public string SqlSelect1()
        {
            string tempstr = "";
            foreach (var t in _pairs)
            {
                if (t.Value == "''" || t.Value == "null" || t.Key.Contains("date") || t.Value.ToLower() == "sysdate") continue;
                tempstr += " and t." + t.Key + " = " + t.Value;
            }

            return "select 1 from " + BaseTarget + "." + TableName + " t where" + tempstr.Substring(4);
        }
    }

    public class SqlConfig : ISqlConfig
    {
        
        /// <summary>
        /// SqlConfig实例对象
        /// </summary>
        private static SqlConfig _Current = null;
        public static SqlConfig Current { get => _Current; set => _Current = value; }
        private SqlConfig(){}

        #region 公开数据结构 
        public enum SqlConfigInitMode { Default, New ,WatchingModify}
        public enum InsertSortMode { Original, ByTableName }
        public class BaseConfig
        {
            [XmlIgnore]
            private string _BaseName = "";

            [XmlIgnore]
            private string _BaseFileNameg = "";

            [XmlIgnore]
            private string _PublicBaseName = "";

            public string BaseName { get => _BaseName; set => _BaseName = value; }
            public string BaseFileNameg { get => _BaseFileNameg; set => _BaseFileNameg = value; }
            public string PublicBaseName { get => _PublicBaseName; set => _PublicBaseName = value; }
        }
        #endregion

        #region 私有字段集
        [XmlIgnore]
        private string _Name = "";

        [XmlIgnore]
        private string _Phone = "";

        [XmlIgnore]
        private InsertSortMode _InsertSqlSortMode = 0;

        [XmlIgnore]
        private List<BaseConfig> _List = new List<BaseConfig>();

        [XmlIgnore]
        private string _FormatSymbol="";
        #endregion


        #region 序列化字段
        public string Name { get => _Name; set => _Name = value; }
        public string Phone { get => _Phone; set => _Phone = value; }
        public InsertSortMode InsertSqlSortMode { get => _InsertSqlSortMode; set => _InsertSqlSortMode = value; }
        public List<BaseConfig> List { get => _List; set => _List = value; }
        public string FormatSymbol { get => _FormatSymbol; set => _FormatSymbol = value; }

        #endregion

        #region 方法集合
        /// <summary>
        /// 配置初始化
        /// </summary>
        /// <param name="InitMode">初始化方式</param>
        public static void InitSqlConfig(SqlConfigInitMode InitMode)
        {
            var _StartupPath = Application.StartupPath;
            if (File.Exists(_StartupPath + "\\config.xml") && InitMode == SqlConfigInitMode.Default)
            {
                string str = "";
                using (Stream s = File.OpenRead(_StartupPath + "\\config.xml"))
                {
                    using (StreamReader sr = new StreamReader(s))
                        str = sr.ReadToEnd();
                }
                Current = Deserialize(str);
            }
            if (InitMode == SqlConfigInitMode.New || (InitMode == SqlConfigInitMode.Default && !File.Exists(_StartupPath + "\\config.xml")))
            {
                using (Stream s = File.Create(_StartupPath + "\\config.xml"))
                {
                    using (StreamWriter sw = new StreamWriter(s))
                    {
                        if(_Current==null)
                        sw.WriteLine(Serialize(CreateEmptyConfig()));
                        else
                            sw.WriteLine(Serialize(_Current));
                    }
                }
            }
            if(InitMode==SqlConfigInitMode.WatchingModify)
            {
                try
                {
                    Thread.Sleep(50);
                    string str = "";
                    using (Stream s = File.OpenRead(_StartupPath + "\\config.xml"))
                    {
                        using (StreamReader sr = new StreamReader(s))
                            str = sr.ReadToEnd();
                    }
                    Current = Deserialize(str);
                }
                catch (Exception ex)
                {
                    Form1.ObjectReference?.SendInfo(ex.Message);
                }

            }
        }

        private static string Serialize(SqlConfig configs)
        {
            XmlSerializer serobj = new XmlSerializer(configs.GetType());
            var vm = new MemoryStream(512);
            serobj.Serialize(vm, configs);
            string res = Encoding.UTF8.GetString(vm.ToArray());
            var index = res.IndexOf("<" + nameof(InsertSqlSortMode) + ">");
            int blankcount = 0;
            while (res[index - ++blankcount] == ' ') { }
            res = res.Insert(index,
                "<!--可用的InsertSqlSortMode值{" + string.Join(",", Enum.GetNames(typeof(InsertSortMode)))
                + "}-->\n"
                + string.Join(" ", new string[blankcount])
                + "<!-- 别使用自动保存 -->\n"
                + string.Join(" ", new string[blankcount]));
            return res;
        }

        private static SqlConfig Deserialize(string str)
        {
            try
            {
                XmlSerializer serobj = new XmlSerializer(typeof(SqlConfig));
                var vm = new MemoryStream(Encoding.UTF8.GetBytes(str));
                return serobj.Deserialize(vm) as SqlConfig;
            }
            //处理并预读文件 保证配置正确
            catch (Exception)
            {
                InitSqlConfig(SqlConfigInitMode.New);
                throw new Exception("配置文件错误 已被重置");
            }
        }

        public string getBaseFileName(string BaseName)
        {
            return (from t in _List
                    where t.BaseName == BaseName
                    select t.BaseFileNameg).ToList()[0];
        }

        private static SqlConfig CreateEmptyConfig()
        {
            SqlConfig Origin = new SqlConfig
            {
                _Name = "名字",
                _Phone = "手机号",
                _InsertSqlSortMode = InsertSortMode.Original,
                _FormatSymbol = '"' + @"`~!@#$%^&*()_+-=[]\;',-../{}|:<>?" + @"·【】、；‘，。/~！@#￥%……&*（）——+{}|：“”《》？ " + "\r\n"
            };
            Origin._List.Add(new BaseConfig()
            {
                BaseName = "uip",
                BaseFileNameg = "ZJPUB_BASE",
                PublicBaseName = @"base@zjpub"
            });
            Origin._List.Add(new BaseConfig()
            {
                BaseName = "base",
                BaseFileNameg = "ZJPUB_BASE",
                PublicBaseName = @"base@zjpub"
            });
            Origin._List.Add(new BaseConfig()
            {
                BaseName = "rule_cfg",
                BaseFileNameg = "ZJZXHC1_CMCONF_RULE_CFG",
                PublicBaseName = @"rule_cfg@ZJZXHC1_CMCONF"
            });
            return Origin;
        }
        #endregion

        #region 配置文件监视
        public static void StartFileWatching()
        {
            FileSystemWatcher fsw = new FileSystemWatcher
            {
                Path = Application.StartupPath 
            };
            fsw.Changed += Fsw_Changed;
            fsw.EnableRaisingEvents = true;
        }

        private static void Fsw_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.Name != "config.xml") return;
            InitSqlConfig(SqlConfigInitMode.WatchingModify);
        }
        #endregion

    }

    public class SqlExcuter
    {
        #region sql文件预读方法组
        private static IEnumerable<string> ExecLines(IList<string> strs)
        {
            List<string> resSet = new List<string>();
            bool isinnotes = false;
            for (int i = 0; i < strs.Count; i++)
            {
                string str = strs[i];

                //处理多行注释结尾
                if (isinnotes)
                {
                    int notesendindex = str.IndexOf(@"*/");
                    if (notesendindex < 0)
                    {
                        resSet.Add(""); continue;
                    }

                    else
                    {
                        isinnotes = false;
                        if (str.Length > 2)
                            str = str.Substring(notesendindex + 2);
                    }
                }

                bool isfindhead = false;

                //处理多行注释头部
                int notesstartindex = str.IndexOf(@"/*");
                if (notesstartindex >= 0)
                    isfindhead = true;

                //处理单行注释
                int onenote = str.IndexOf("--");
                if (onenote == 0) { resSet.Add(""); continue; }
                else if (onenote > 0) str = str.Substring(0, onenote);


                //处理单行注释结尾
                if (isfindhead)
                {
                    int notesendindex = str.IndexOf(@"*/");
                    if (notesendindex > notesstartindex)
                    {
                        resSet.Add(""); continue;
                    }

                    else
                    {
                        isinnotes = false;
                        if (str.Length > 2)
                            str = str.Substring(notesendindex + 2);
                    }
                }

                //通过项加入集合
                //如果没有内容会加入一个空的字符串
                resSet.Add(str);

            }
            return resSet;
        }


        private enum SupportEncoding { UTF8, GBK, GB2312 }

        /// <summary>
        /// 用于阻止溢栈
        /// </summary>
        static int RecursiveCount = 0;

        private static bool CheckEncoding(string str)
        {
            foreach (var t in str)
                if (!((t >= 0x4E00 && t <= 0x9FCB) || (t >= 0x30 && t <= 0x39) || (t >= 0x61 && t <= 0x7a) || (t >= 0x41 && t <= 0x5a) || SqlConfig.Current.FormatSymbol.Contains(t)))
                    return false;
            return true;
        }


        /// <summary>
        /// 自动切换GBK或UTF-8编码 去除注释
        /// </summary>
        /// <param name="filepath">文件路径</param>
        /// <returns>以IEnumerable<string>形式返回处理后的文件</returns>
        private static IEnumerable<string> preLoad(string filepath)
        {
            RecursiveCount++;
            if (RecursiveCount > Enum.GetValues(typeof(SupportEncoding)).Length) return null;
            List<string> resSet = new List<string>();
            using (var fis = new StreamReader(new FileStream(filepath, FileMode.Open)))
            {
                while (!fis.EndOfStream)
                {
                    var str = fis.ReadLine();
                    if (!CheckEncoding(str))
                    {
                        fis.Dispose();
                        return preLoad(filepath, Encoding.GetEncoding("GBK"));
                    }
                    resSet.Add(str);
                }
            }
            return ExecLines(resSet);
        }

        /// <summary>
        /// 递归方法 已阻止溢栈
        /// </summary>
        /// <exception cref="FileEncodingExcption">在没有匹配到合适的编码时会抛出</exception>
        private static IEnumerable<string> preLoad(string filepath, Encoding encoding)
        {
            RecursiveCount++;
            if (RecursiveCount > Enum.GetValues(typeof(SupportEncoding)).Length) return null;
            List<string> resSet = new List<string>();
            using (var fis = new StreamReader(filepath, encoding))
            {
                while (!fis.EndOfStream)
                {
                    string str = fis.ReadLine();
                    if (!CheckEncoding(str))
                    {
                        fis.Dispose();
                        return preLoad(filepath, Encoding.GetEncoding("GB2312"));
                    }
                    resSet.Add(str);
                }
            }
            return ExecLines(resSet);
        }

        public class FileEncodingExcption:Exception
        {
            public override string Message => "File Encoding Error";
        }

        private static string combineStrs(IEnumerable<string> strs)
        {
            string res = "";
            foreach (var t in strs)
                res += t;
            return res;
        }

        /// <summary>
        /// 切分出insert语句
        /// </summary>
        /// <param name="strs">无注释的文件</param>
        /// <returns>insert语句集合</returns>
        private static IEnumerable<string> SplitString(IEnumerable<string> strs)
        {
            string str = combineStrs(strs);
            if (str == "") return null;

            List<string> resSet = new List<string>();

            //切割insert
            string lowerstr = str.ToLower();
            int insertIndex = lowerstr.IndexOf("insert");
            while (insertIndex > -1)
            {
                int fqurt = lowerstr.Substring(insertIndex).IndexOf(";");
                if (fqurt < 0) throw new Exception("sql syntax error");
                else
                {
                    resSet.Add(str.Substring(insertIndex, 1 + fqurt));
                    str = str.Substring(fqurt + 1);
                    lowerstr = lowerstr.Substring(fqurt + 1);
                    insertIndex = lowerstr.IndexOf("insert");
                }
            }







            return resSet;
        }

        private static SqlRow SplitSingleInsert(string insertsql)
        {
            SqlRow res = new SqlRow();
            int targetindex = insertsql.IndexOf('.');
            int baseindex = 0;
            for (int i = targetindex - 1; i > -1; i--)
                if (insertsql[i] == ' ')
                {
                    baseindex = i;
                    break;
                }
            int tableindex = 0;
            for (int i = targetindex + 1; i < insertsql.Length; i++)
                if (insertsql[i] == ' ')
                {
                    tableindex = i;
                    break;
                }
            res.BaseTarget = insertsql.Substring(baseindex + 1, targetindex - baseindex - 1);
            res.TableName = insertsql.Substring(targetindex + 1, tableindex - targetindex - 1);



            int token1 = insertsql.IndexOf("(");
            int token2 = insertsql.IndexOf(")");
            if (token1 < 0 || token2 < 0) throw new Exception("sql syntax error");
            string substr = insertsql.Substring(token1 + 1, token2 - token1 - 1);
            string[] strs1 = substr.Split(',');
            for (int i = 0; i < strs1.Length; i++)
            {
                strs1[i] = strs1[i].Trim(' ');
                strs1[i] = strs1[i].ToLower();
            }


            insertsql = insertsql.Substring(token2 + 1);
            token1 = insertsql.IndexOf("(");
            token2 = insertsql.LastIndexOf(")");
            if (token1 < 0 || token2 < 0) throw new Exception("sql syntax error");
            substr = insertsql.Substring(token1 + 1, token2 - token1 - 1);
            string[] strs2 = substr.Split(new char[] { ',' }, StringSplitOptions.None);

            List<string> templis = strs2.ToList();
            List<string> removelist = new List<string>();


            int templis_token = 0;
            int appendtarget = 0;
            for (int i = 0; i < templis.Count; i++)
            {
                if (templis_token > 0)
                {
                    int count = 0;
                    foreach (var t in templis[i])
                        if (t == ')') count++;
                    if (count > 0 && count <= templis_token)
                    {
                        templis_token -= count;
                        templis[appendtarget] += "," + templis[i];
                        removelist.Add(templis[i]);
                    }
                    else if (count > templis_token)
                        if (token1 < 0 || token2 < 0) throw new Exception("sql syntax error");
                }

                int count1 = 0;
                int count2 = 0;
                foreach (var t in templis[i])
                {
                    if (t == '(') count1++;
                    else if (t == ')') count2++;
                }
                int count3 = count1 - count2;
                if (count3 > 0)
                {
                    templis_token = count3;
                    appendtarget = i;
                }
                if (count3 == 0 && templis_token == 0)
                    templis[i] = templis[i].Trim(' ');
            }
            foreach (var t in removelist)
                templis.Remove(t);
            strs2 = templis.ToArray();

            removelist = new List<string>();
            int combingtarget = 0;
            int combinecount = 0;
            for (int i = 0; i < strs2.Length; i++)
            {
                if (combinecount > 0)
                {
                    removelist.Add(strs2[i]);
                    strs2[combingtarget] += "," + strs2[i];

                    int dotcount = 0;
                    foreach (var t1 in strs2[i])
                        if (t1 == "'"[0]) dotcount++;
                    if (dotcount % 2 != 0)
                        combinecount--;
                }
                else
                {
                    int dotcount = 0;
                    foreach (var t1 in strs2[i])
                        if (t1 == "'"[0]) dotcount++;
                    if (dotcount % 2 != 0)
                    {
                        combinecount++;
                        combingtarget = i;
                    }
                }
            }
            List<string> strs2RemoveList = strs2.ToList();
            foreach (var t in removelist)
                strs2RemoveList.Remove(t);
            strs2 = strs2RemoveList.ToArray();




            if (strs1.Length != strs2.Length) throw new Exception("sql syntax error");

            for (int i = 0; i < strs1.Length; i++)
            {
                if (strs1[i] != "rowid")
                {
                    res.PairCollection.Add(new SqlRow.SqlRow_Union() { Key = strs1[i], Value = strs2[i] });
                }
            }

            return res;
        }

        private static IEnumerable<string> combinValue(char s1, char s2, IEnumerable<string> strs)
        {
            var templis = strs.ToList();
            List<string> removelist = new List<string>();
            int templis_token = 0;
            int appendtarget = 0;
            for (int i = 0; i < templis.Count; i++)
            {
                if (templis_token > 0)
                {
                    int count = 0;
                    foreach (var t in templis[i])
                        if (t == s2) count++;
                    if (count > 0 && count <= templis_token)
                    {
                        templis_token -= count;
                        templis[appendtarget] += "," + templis[i];
                        removelist.Add(templis[i]);
                    }
                }

                int count1 = 0;
                int count2 = 0;
                foreach (var t in templis[i])
                {
                    if (t == s1) count1++;
                    else if (t == s2) count2++;
                }
                int count3 = count1 - count2;
                if (count3 > 0)
                {
                    templis_token = count3;
                    appendtarget = i;
                }
                if (count3 == 0 && templis_token == 0)
                    templis[i] = templis[i].Trim(' ');
            }
            foreach (var t in removelist)
                templis.Remove(t);

            return templis;
        }


        public static List<KeyValuePair<string, List<SqlRow>>> OrderPairs = null;
        /// <summary>
        /// 将insert转化成sql对象
        /// </summary>
        /// <param name="inserts">insert语句</param>
        /// <returns>insert对象</returns>
        public static IEnumerable<SqlRow> SplitInsert(IEnumerable<string> inserts)
        {
            List<SqlRow> resSet = new List<SqlRow>();
            foreach (var t in inserts)
                resSet.Add(SplitSingleInsert(t));


            OrderPairs = new List<KeyValuePair<string, List<SqlRow>>>();
            List<SqlRow> errList = new List<SqlRow>();
            foreach (var t in resSet)
            {
                bool findtable = false;
                foreach (var tt in OrderPairs)
                    if (tt.Key == t.TableName)
                    {
                        findtable = true;
                        bool finddata = false;
                        foreach (var ttt in tt.Value)

                            if (ttt.PairCollection[0].Value == t.PairCollection[0].Value && ttt.BaseTarget == t.BaseTarget)
                            {
                                finddata = true;
                                errList.Add(t);
                                break;
                            }

                        if (!finddata)
                            tt.Value.Add(t);
                    }
                if (!findtable)
                    OrderPairs.Add(new KeyValuePair<string, List<SqlRow>>(t.TableName, new List<SqlRow>() { t }));
            }

            if (errList.Count > 0)
            {
                string mes = "";
                foreach (var t in errList)
                    mes += "Error Unique at " + t.BaseTarget + "." + t.TableName + "." + t.PairCollection[0] + "\n";
                throw new Exception(mes);
            }

            if (SqlConfig.Current.InsertSqlSortMode == SqlConfig.InsertSortMode.ByTableName)
            {
                resSet.Clear();
                foreach (var t in OrderPairs)
                {
                    foreach (var tt in t.Value)
                    {
                        resSet.Add(tt);
                    }
                }
            }


            return resSet;
        }
        #endregion

        #region 创建方法组
        public static IEnumerable<string> CreateInsert(IEnumerable<SqlRow> sqls)
        {
            List<string> res = new List<string>();
            foreach (var t in sqls)
                res.Add(t.SqlInsert());
            return res;
        }

        public static IEnumerable<string> CreateReset(IEnumerable<SqlRow> sqls)
        {
            List<string> res = new List<string>();
            foreach (var t in sqls)
                res.Add(t.SqlResetUpdate());
            return res;
        }

        public static IEnumerable<string> CreateRollback(IEnumerable<SqlRow> sqls)
        {
            List<string> res = new List<string>();
            foreach (var t in sqls)
                res.Add(t.SqlRollbackDelete());
            return res;
        }

        public static string CreateCheck(IEnumerable<SqlRow> sqls)
        {
            string res = "declare a int;b int; begin b:=0;\n";
            foreach (var t in sqls)
                res += "\n  select count(1) into a from( " + t.SqlSelect1() + ");b:=a+b;\n      if a=0 then dbms_output.put_line('Error at " + t.BaseTarget + "." + t.TableName + "." + t.PairCollection[0] + "');else dbms_output.put_line('Pass " + t.BaseTarget + "." + t.TableName + "." + t.PairCollection[0] + "');end if;";
            res += "\n      if b = " + sqls.Count() + " then dbms_output.put_line('Check Result:True') ;else dbms_output.put_line('Check Result:False'); end if;end;";
            return res;
        }
        #endregion

        /// <summary>
        /// 公开静态方法
        /// </summary>
        /// <param name="filepaths">源文件路径</param>
        /// <param name="filesavepath">目标文件路径</param>
        public static void Excute(IEnumerable<string> filepaths, string filesavepath)
        {
            int f_index = -1;
            //try
            //{
            foreach (var filepath in filepaths)
            {
                f_index++;


                if (!Directory.Exists(filesavepath + "\\Rollback"))
                    Directory.CreateDirectory(filesavepath + "\\Rollback");

                List<string> basekinds = new List<string>();
                var r1 = SqlExcuter.preLoad(filepath);
                var r2 = SqlExcuter.SplitString(r1);
                var r3 = SqlExcuter.SplitInsert(r2);

                Form1.ObjectReference?.SendInfo("Read sql file");

                foreach (var t in r3)
                    if (!basekinds.Contains(SqlConfig.Current.getBaseFileName(t.BaseTarget)))
                        basekinds.Add(SqlConfig.Current.getBaseFileName(t.BaseTarget));


                int g_index = -1;
                foreach (var t in basekinds)
                {
                    g_index++;
                    var baseconfigs = from t2 in SqlConfig.Current.List
                                      where t2.BaseFileNameg == t
                                      select t2;
                    if (baseconfigs.Count() < 1)
                        throw new Exception("config error");
                    var baseconfig = baseconfigs.ElementAt(0);


                    var res = from t1 in r3
                              where SqlConfig.Current.getBaseFileName(t1.BaseTarget) == t
                              select t1;

                    Form1.ObjectReference?.SendInfo("Output filepath"+ filesavepath);
                    var r4 = SqlExcuter.CreateInsert(res);
                    if (!Directory.Exists(filesavepath + "\\Insert"))
                        Directory.CreateDirectory(filesavepath + "\\Insert");
                    string insertname = f_index + "_" + g_index + "_" + baseconfig.BaseFileNameg + "_INSERT_TSK_" + SqlConfig.Current.Name + "_" + SqlConfig.Current.Phone + "_配置脚本.sql";
                    using (Stream s = File.Create(filesavepath + "\\Insert\\" + insertname))
                    {
                        using (StreamWriter sw = new StreamWriter(s))
                        {
                            List<string> heads = new List<string>() { "--目标数据库:" + baseconfig.PublicBaseName, "--脚本说明：", "--父脚本：无", "--提前执行：是", "set define off;", "alter session set current_schema = " + baseconfig.BaseName + ";" };
                            foreach (var t4 in heads)
                            {
                                sw.WriteLine(t4);
                            }
                            foreach (var t4 in r4)
                            {
                                sw.WriteLine(t4);
                            }
                        }

                    }

                    var r5 = SqlExcuter.CreateReset(res);
                    if (!Directory.Exists(filesavepath + "\\Reset"))
                        Directory.CreateDirectory(filesavepath + "\\Reset");
                    string resetname = f_index + "_" + g_index + "_" + baseconfig.BaseFileNameg + "_UPDATE_TSK_" + SqlConfig.Current.Name + "_" + SqlConfig.Current.Phone + "_重置脚本.sql";
                    using (Stream s = File.Create(filesavepath + "\\Reset\\" + resetname))
                    {
                        using (StreamWriter sw = new StreamWriter(s))
                        {
                            List<string> heads = new List<string>() { "--目标数据库:" + baseconfig.PublicBaseName, "--脚本说明：", "--父脚本：无", "--提前执行：是", "set define off;", "alter session set current_schema = " + baseconfig.BaseName + ";" };
                            foreach (var t4 in heads)
                            {
                                sw.WriteLine(t4);
                            }
                            foreach (var t4 in r5)
                            {
                                sw.WriteLine(t4);
                            }
                        }

                    }

                    var r6 = SqlExcuter.CreateRollback(res);

                    if (!Directory.Exists(filesavepath + "\\Rollback"))
                        Directory.CreateDirectory(filesavepath + "\\Rollback");
                    string rollbackname = f_index + "_" + g_index + "_" + baseconfig.BaseFileNameg + "_DELETE_TSK_" + SqlConfig.Current.Name + "_" + SqlConfig.Current.Phone + "_回退脚本.sql";
                    using (Stream s = File.Create(filesavepath + "\\Rollback\\" + rollbackname))
                    {
                        using (StreamWriter sw = new StreamWriter(s))
                        {
                            List<string> heads = new List<string>() { "--目标数据库:" + baseconfig.PublicBaseName, "--脚本说明：", "--父脚本：无", "--提前执行：是", "set define off;", "alter session set current_schema = " + baseconfig.BaseName + ";" };
                            foreach (var t4 in heads)
                            {
                                sw.WriteLine(t4);
                            }
                            foreach (var t4 in r6)
                            {
                                sw.WriteLine(t4);
                            }
                        }

                    }

                    var r7 = SqlExcuter.CreateCheck(res);
                    if (!Directory.Exists(filesavepath + "\\Check"))
                        Directory.CreateDirectory(filesavepath + "\\Check");
                    string checkname = f_index + "_" + g_index + "_" + baseconfig.BaseFileNameg + "_SELECT_TSK_" + SqlConfig.Current.Name + "_" + SqlConfig.Current.Phone + "_检查脚本.sql";
                    using (Stream s = File.Create(filesavepath + "\\Check\\" + checkname))
                    {
                        using (StreamWriter sw = new StreamWriter(s))
                        {
                            List<string> heads = new List<string>() { "--目标数据库:" + baseconfig.PublicBaseName, "--脚本说明：", "--父脚本：无", "--提前执行：是", "set define off;", "alter session set current_schema = " + baseconfig.BaseName + ";" };
                            foreach (var t4 in heads)
                            {
                                sw.WriteLine(t4);
                            }
                            sw.WriteLine(r7);
                        }


                    }
                    Form1.ObjectReference?.SendInfo("Output complete" );
                }

            }

            //}
            //catch (Exception e)
            //{
            //    MessageBox.Show(e.Message);
            //}

        }



    }
}
