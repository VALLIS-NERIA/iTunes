using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace NeteaseLib {
    public class NeteaseResponse {
        public NeteaseResult Result { get; set; }
        public override string ToString() { return Result.ToString(); }
    }

    public class NeteaseResult {
        public List<NeteaseSong> Songs { get; set; }

        public override string ToString() {
            var sb = new StringBuilder();
            foreach (var song in Songs) {
                sb.Append(song + Environment.NewLine);
            }
            return sb.ToString();
        }
    }

    [DebuggerDisplay("{Artist} - {Name}")]
    public class NeteaseSong {
        public long Id { get; set; }
        public List<NeteaseArtist> Artists { get; set; }
        public string Name { get; set; }
        public string Artist => Artists.Count == 1 ? Artists[0].Name : $"{Artists[0].Name}(+{Artists.Count - 1})";
        public static implicit operator string(NeteaseSong me) => me.ToString();
        public override string ToString() => $"{Artist} - {Name}";
    }

    [DebuggerDisplay("{Name}")]
    public class NeteaseArtist {
        public string Name { get; set; }
        public static implicit operator string(NeteaseArtist me) => me.ToString();
        public override string ToString() => Name;
    }
}