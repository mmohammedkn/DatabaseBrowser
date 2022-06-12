using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseBrowser.Models
{
    internal class DBObject
    {
        public DBObject()
        {

        }

        public DBType Type { get; set; }
        public string Name { get; set; }
        public SavedConnection DBConnection { get; set; }

        public override bool Equals(object obj)
        {
            return (obj as DBObject).Name == Name && (obj as DBObject).Type == Type;
        }
    }

    public enum DBType
    {
        Table,
        View,
        Package
    }
}
