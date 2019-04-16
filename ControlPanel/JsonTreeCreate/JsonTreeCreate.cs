using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace ControlPanel.JsonTreeCreate
{
    public static class JsonTreeCreate
    {
        public static JObject ToJson<TResult>(this DirectoryInfo info, Func<FileInfo, TResult> getData)
        {
            return new JObject
            (
                info.GetFiles().Select(f => new JProperty(f.Name, getData(f))).Concat(info.GetDirectories().Select(d => new JProperty(d.Name, d.ToJson(getData))))
            );
        }
    }
}