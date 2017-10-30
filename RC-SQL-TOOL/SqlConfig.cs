using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace RC_SQL_TOOL
{
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

        [XmlIgnore]
        private List<string> _PKignoreTable = new List<string>();
        #endregion


        #region 序列化字段
        public string Name { get => _Name; set => _Name = value; }
        public string Phone { get => _Phone; set => _Phone = value; }
        public InsertSortMode InsertSqlSortMode { get => _InsertSqlSortMode; set => _InsertSqlSortMode = value; }
        public List<BaseConfig> List { get => _List; set => _List = value; }
        public string FormatSymbol { get => _FormatSymbol; set => _FormatSymbol = value; }
        public List<string> PKignoreTable { get => _PKignoreTable; set => _PKignoreTable = value; }

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
                SaveConfig();
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

        public static void SaveConfig()
        {
            using (Stream s = File.Create(Application.StartupPath + "\\config.xml"))
            {
                using (StreamWriter sw = new StreamWriter(s))
                {
                    if (_Current == null)
                        sw.WriteLine(Serialize(CreateEmptyConfig()));
                    else
                        sw.WriteLine(Serialize(_Current));
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
                var serobj = new XmlSerializer(typeof(SqlConfig));
                var vm = new MemoryStream(Encoding.UTF8.GetBytes(str));
                var sqlconfig= serobj.Deserialize(vm) as SqlConfig;
                var tstrs = new List<string>();
                foreach (var t in sqlconfig._PKignoreTable)
                    tstrs.Add(t.ToLower());
                sqlconfig._PKignoreTable = tstrs;
                return sqlconfig;
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
                _PKignoreTable = { "bs_para_detail" },
                _InsertSqlSortMode = InsertSortMode.Original,
                _FormatSymbol = '"' + @"`~!@#$%^&*()_+-=[]\;',-../{}|:<>?" + @"·【】、；‘，。/~！@#￥%……&*（）——+{}|：“”《》？ " + "\r\n\t"
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
}
