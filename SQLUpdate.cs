using System.Collections;
using Microsoft.Data.Sqlite;
using Owl;

enum SQLActionTypes
{
    NEW_COMMAND,
    ADD_COMMAND_PARAMS,
    OPEN_CONN,
    EXEC_NON_QUERY,
    EXEC_SCALAR,
    EXEC_READER,
    CLOSE_CONN,
    BATCH,
    RESPONCE
}

abstract class AbstractSQLMsg
{
    public int? Sender {get; set;}
    public SQLActionTypes UpdType {get; protected set;}
}

class ResponceMsg : AbstractSQLMsg
{
    public readonly object? Obj;
    public ResponceMsg(string s) => Obj = s;
    public ResponceMsg(object o) => Obj = o;
    public ResponceMsg(List<List<object>> sl) => Obj = sl;
}

class SQLMsg : AbstractSQLMsg
{
    public readonly string? strone;
    public readonly object? objtwo;

    public SQLMsg(SQLActionTypes typ) => UpdType = typ;
    public SQLMsg(SQLActionTypes typ, string s1) => (UpdType, strone) = (typ, s1);
    public SQLMsg(SQLActionTypes typ, string s1, object o2) => (UpdType, strone, objtwo) = (typ, s1, o2);
}

class SQLMsgBatch : AbstractSQLMsg, IEnumerable
{
    private List<SQLMsg> msgs;
    public SQLMsgBatch()
    {
        msgs = new();
        UpdType = SQLActionTypes.BATCH;
    }
    public void Add(SQLMsg m) => msgs.Add(m);
    public IEnumerator GetEnumerator()
    {
        for (int i = 0; i < msgs.Count; i++)
            yield return msgs[i];
    }
}

class SQLUpdateRunner : UpdateRunner<AbstractSQLMsg>
{
    private SqliteConnection conn;

    private SqliteCommand? cmd;

    public SQLUpdateRunner(string conns) => conn = new(conns);

    public override void Run(InputOnlyQueue<AbstractSQLMsg> i, OutputOnlyQueue<AbstractSQLMsg> o)
    {
        while (i.Count > 0 && !TSource.IsCancellationRequested)
        {
            AbstractSQLMsg? msg;
            if (!i.TryDequeue(out msg)) continue;

            ResponceMsg? responce;
            switch (msg)
            {
                case SQLMsg:
                    responce = HandleMsg((SQLMsg)msg);
                    if (responce is not null) o.Enqueue(responce);
                    break;
                case SQLMsgBatch:
                    foreach (var m in (SQLMsgBatch)msg)
                    {
                        responce = HandleMsg((SQLMsg)m);
                        if (responce is not null) o.Enqueue(responce);
                    }
                    break;
                default: throw new InvalidDataException("Got an unknown action type");
            }
        }
    }

    public override void Dispose()
    {
        conn.Dispose();
        base.Dispose();
    }

    private ResponceMsg? HandleMsg(SQLMsg msg)
    {
        switch (msg.UpdType)
        {
            case SQLActionTypes.NEW_COMMAND:
                cmd = new();
                cmd.CommandText = msg.strone;
                return null;
            case SQLActionTypes.ADD_COMMAND_PARAMS:
                cmd!.Parameters.AddWithValue(msg.strone, msg.objtwo);
                return null;
            case SQLActionTypes.OPEN_CONN:
                conn!.Open();
                return null;
            case SQLActionTypes.EXEC_NON_QUERY:
                cmd!.ExecuteNonQuery();
                return null;
            case SQLActionTypes.EXEC_SCALAR:
                return new ResponceMsg(cmd!.ExecuteScalar()!);
            case SQLActionTypes.EXEC_READER:
                var reader = cmd!.ExecuteReader();
                var sl = new List<List<object>>();
                while (reader.Read())
                {
                    var row = new List<object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                        row.Add(reader.GetValue(i));
                    sl.Add(row);
                }
                return new ResponceMsg(sl);
            case SQLActionTypes.CLOSE_CONN:
                conn!.Close();
                return null;
            default: throw new InvalidDataException("Got an unknown action");

        }
    }
}
