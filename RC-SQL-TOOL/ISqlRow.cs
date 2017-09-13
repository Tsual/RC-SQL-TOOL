using System.Collections.Generic;

namespace RC_SQL_TOOL
{
    public interface ISqlRow
    {
        string BaseTarget { get; set; }
        List<SqlRow.SqlRow_Union> PairCollection { get; set; }
        string TableName { get; set; }

        string SqlInsert();
        string SqlResetUpdate();
        string SqlRollbackDelete();
        string SqlSelect1();
        string ToString();
    }
}