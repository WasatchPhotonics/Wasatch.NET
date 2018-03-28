using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace APITest
{
    class CommandFactory
    {
        static Logger logger = Logger.getInstance();

        public class JsonCommand
        {
            public string Opcode { get; set; }
            public string Direction { get; set; }
            public string Type { get; set; }
            public int Length { get; set; }
            public string wValue { get; set; }
            public string wIndex { get; set; }
            public string Units { get; set; }
            public IList<string> Uses { get; set; }
            public IList<string> Enum { get; set; }
            public IList<string> Supports { get; set; }
            public bool MakeFakeBufferFromValue { get; set; }
            public bool ARMInvertedReturn { get; set; }
            public int ReadBack { get; set; }
            public int ReadBackARM { get; set; }
            public int ReadBlockSize { get; set; }
            public int FakeBufferLength { get; set; }
            public int ReadEndpoint { get; set; }
            public bool Reverse { get; set; }
            public bool Enabled { get; set; }
            public string Notes { get; set; }
        }

        public static SortedDictionary<string, Command> loadCommands(string pathname)
        {
            string text = File.ReadAllText(pathname);
            SortedDictionary <string, JsonCommand> jsonCommands = JsonConvert.DeserializeObject< SortedDictionary<string, JsonCommand> >(text);

            if (jsonCommands == null)
            {
                logger.error("could not deserialize {0}", pathname);
                return null;
            }

            SortedDictionary<string, Command> commands = new SortedDictionary<string, Command>();
            foreach (KeyValuePair<string, JsonCommand> pair in jsonCommands)
            {
                string name = pair.Key;
                JsonCommand jcmd = pair.Value;
                logger.debug("loaded {0} (opcode {1}, type {2})", name, jcmd.Opcode, jcmd.Type);

                try
                {
                    Command cmd = createCommand(name, jcmd);
                    commands[name] = cmd;
                }
                catch(Exception ex)
                {
                    logger.error("Couldn't parse JSON command {0}: {1}", name, ex);
                }
            }
            return commands;
        }

        static Command createCommand(string name, JsonCommand jcmd)
        {
            Command cmd = new Command();

            cmd.name = name;

            cmd.opcode = (byte)Util.fromHex(jcmd.Opcode);
            if (jcmd.wValue != null)
            {
                cmd.fixedWValue = true;
                cmd.wValue = Util.fromHex(jcmd.wValue);
            }
            if (jcmd.wIndex != null)
            {
                cmd.wIndex = Util.fromHex(jcmd.wIndex);
                cmd.fixedWIndex = true;
            }

            cmd.dataType = convertDataType(jcmd.Type);
            cmd.direction = jcmd.Direction.ToUpper().StartsWith("HOST") ? Command.Direction.HOST_TO_DEVICE : Command.Direction.DEVICE_TO_HOST;

            cmd.units = jcmd.Units;
            cmd.length = jcmd.Length;
            cmd.readBack = jcmd.ReadBack;
            cmd.readBackARM = jcmd.ReadBackARM;
            cmd.readBlockSize = jcmd.ReadBlockSize;
            cmd.enabled = jcmd.Enabled;
            cmd.notes = jcmd.Notes;
            cmd.armInvertedReturn = jcmd.ARMInvertedReturn;
            cmd.reverse = jcmd.Reverse;
            cmd.readEndpoint = jcmd.ReadEndpoint;
            cmd.makeFakeBufferFromValue = jcmd.MakeFakeBufferFromValue;
            cmd.fakeBufferLength = jcmd.FakeBufferLength;

            if (jcmd.Supports != null && jcmd.Supports.Count > 0)
            {
                cmd.supportsBoards = new HashSet<string>();
                foreach (string s in jcmd.Supports)
                    cmd.supportsBoards.Add(s);
            }

            if (jcmd.Uses != null && jcmd.Uses.Count > 0)
            {
                cmd.usesFields = new HashSet<Command.UsableFields>();
                foreach (string s in jcmd.Uses)
                    cmd.usesFields.Add(s.ToUpper() == "WVALUE" ? Command.UsableFields.WVALUE : Command.UsableFields.WINDEX);
            }

            if (jcmd.Enum != null && jcmd.Enum.Count > 0)
            {
                cmd.enumValues = new List<string>();
                foreach (string s in jcmd.Enum)
                    cmd.enumValues.Add(s);
            }

            return cmd;
        }

        static Command.DataType convertDataType(string s)
        {
            s = s.ToUpper();
            if (s == "BOOL") return Command.DataType.BOOL;
            else if (s == "BYTE[]") return Command.DataType.BYTE_ARRAY;
            else if (s == "ENUM") return Command.DataType.ENUM;
            else if (s == "FLOAT16") return Command.DataType.FLOAT16;
            else if (s == "STRING") return Command.DataType.STRING;
            else if (s == "UINT8") return Command.DataType.UINT8;
            else if (s == "UINT12") return Command.DataType.UINT12;
            else if (s == "UINT16") return Command.DataType.UINT16;
            else if (s == "UINT32") return Command.DataType.UINT32;
            else if (s == "UINT40") return Command.DataType.UINT40;
            else if (s == "UINT16[]") return Command.DataType.UINT16_ARRAY;
            else throw new Exception(String.Format("Unrecognized DataType {0}", s));
        }

        public static void dumpCommands(string pathname)
        {
            string json = File.ReadAllText(pathname);
            JsonTextReader reader = new JsonTextReader(new StringReader(json));
            while (reader.Read())
                if (reader.Value != null)
                    logger.info("Token: {0}, Value: {1}", reader.TokenType, reader.Value);
                else
                    logger.info("Token: {0}", reader.TokenType);
        }
    }
}
