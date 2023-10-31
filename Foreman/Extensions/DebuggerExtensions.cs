using System.Diagnostics;
using NLua;
using System.Reflection;

namespace Foreman.Extensions;

using System.Collections.Generic;

[DebuggerDisplay("LuaTable[{Keys.Count}]")]
[DebuggerTypeProxy(typeof(LuaTableDebugView))]
public class MyLuaTable : LuaTable
{
    public MyLuaTable(LuaTable luaTable): 
        base(luaTable.GetHashCode(), Extract<Lua>(luaTable, "_Interpreter")){}
    private MyLuaTable(int reference, Lua interpreter) : base(reference, interpreter) { }

    private new object this[object key] => 
        base[key] is LuaTable ? new MyLuaTable(base[key].GetHashCode(), _Interpreter) : base[key];
    
    private static T Extract<T>(LuaTable table, string fieldName)
    {
        var fieldInfo = typeof(LuaTable).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        return (T)fieldInfo?.GetValue(table)!;
    }
    
    private class LuaTableDebugView
    {
        private MyLuaTable luaTable;
        
        public LuaTableDebugView(MyLuaTable luaTable)
        {
            this.luaTable = luaTable;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public IEnumerable<KeyValuePair> Keys
        {
            get
            {
                KeyValuePair[] keys = new KeyValuePair[luaTable.Keys.Count];
                int i = 0;
                foreach (object key in luaTable.Keys) {
                    keys[i++] = new KeyValuePair(key, luaTable[key]);
                }
                return keys;
            }
        }
    }
}

[DebuggerDisplay(" {value}", Name = "[{key}] ")]
internal class KeyValuePair
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private object key;

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    private object value;

    public KeyValuePair(object key, object value)
    {
        this.key = key;
        this.value = value;
    }
}
    

