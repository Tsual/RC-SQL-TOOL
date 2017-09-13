using System.Collections.Generic;

namespace RC_SQL_TOOL
{
    public interface ISqlConfig
    {
        string FormatSymbol { get; set; }
        SqlConfig.InsertSortMode InsertSqlSortMode { get; set; }
        List<SqlConfig.BaseConfig> List { get; set; }
        string Name { get; set; }
        string Phone { get; set; }

        string getBaseFileName(string BaseName);
    }
}