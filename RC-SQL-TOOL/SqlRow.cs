using System;
using System.Collections.Generic;
using System.Linq;

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
            res += " where " + _pairs[0].Key + " = " + _pairs[0].Value + ";";

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
}
