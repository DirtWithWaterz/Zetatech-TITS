// TerminalCommand.cs lives next to HackerTerminal.cs, dont move it, i fucked that up before and unity freaked out

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Profiling;

namespace CyberpunkTerminal
{
    public interface ITerminalCommand
    {
        string Name { get; }
        string Description { get; }
        IEnumerator Execute(HackerTerminal terminal, string[] args);
    }


    // lies about network latency
    public class PingCommand : ITerminalCommand
    {
        public string Name => "ping";
        public string Description => "Probe a node address for response.";
        public IEnumerator Execute(HackerTerminal t, string[] args)
        {
            string target = args.Length > 1 ? args[1] : "192.168.0.1";
            yield return t.PrintDim($"PING {target} — initiating...");
            for (int i = 0; i < 4; i++)
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(0.2f, 0.5f));
                int ms = UnityEngine.Random.Range(4, 240);
                string tag = ms < 80 ? "[OK]" : ms < 160 ? "[LAG]" : "[!!]";
                yield return t.PrintDim($"  reply from {target}: seq={i+1} time={ms}ms {tag}");
            }
            yield return t.PrintNormal("Ping complete.");
            yield return t.PrintBlank();
        }
    }


    // pretends to find computers. very convincing
    public class ScanCommand : ITerminalCommand
    {
        public string Name => "scan";
        public string Description => "Scan local subnet for active nodes.";
        static readonly string[] Hosts = {
            "ghost-relay-04", "null-router", "yama-proxy-7",
            "anon-bridge-11", "dark-egress", "burner-node-88"
        };
        static readonly int[] Ports = { 22, 80, 443, 1337, 3000, 8080, 9999 };
        public IEnumerator Execute(HackerTerminal t, string[] args)
        {
            yield return t.PrintDim("Scanning subnet 192.168.0.0/24...");
            yield return new WaitForSeconds(0.4f);
            int count = UnityEngine.Random.Range(3, 6);
            for (int i = 0; i < count; i++)
            {
                string ip   = $"192.168.0.{UnityEngine.Random.Range(2, 254)}";
                string host = Hosts[UnityEngine.Random.Range(0, Hosts.Length)];
                int    port = Ports[UnityEngine.Random.Range(0, Ports.Length)];
                yield return t.PrintDim($"  {ip,-16} {host,-20} :{port}  [OPEN]");
                yield return new WaitForSeconds(UnityEngine.Random.Range(0.05f, 0.2f));
            }
            yield return t.PrintNormal($"Scan complete. {count} nodes found.");
            yield return t.PrintBlank();
        }
    }


    //tells you who you are in case you forgot ;3
    public class WhoAmICommand : ITerminalCommand
    {
        public string Name => "whoami";
        public string Description => "Display current session identity.";
        public IEnumerator Execute(HackerTerminal t, string[] args)
        {
            yield return t.PrintNormal($"Runner:    {t.Username}");
            yield return t.PrintNormal($"Node:      {t.RoomCode}");
            yield return t.PrintDim("Clearance: PROVISIONAL");
            yield return t.PrintDim("Trust:     UNVERIFIED");
            yield return t.PrintBlank();
        }
    }


    // generates random system stats
    public class StatusCommand : ITerminalCommand
    {
        public string Name => "status";
        public string Description => "Display system and connection status.";

        public IEnumerator Execute(HackerTerminal t, string[] args)
        {



            yield return t.PrintDim("System status snapshot:");

            float millis = Environment.TickCount;
            float uptimeHrs = millis / 1000f / 60f / 60f;
            float uptimeMins = Mathf.RoundToInt((uptimeHrs - Mathf.Floor(uptimeHrs)) * 60f);
            uptimeHrs = Mathf.RoundToInt(uptimeHrs);

            yield return t.PrintDim($"  Uptime:      {uptimeHrs}h {uptimeMins}m");

            ProfilerRecorder threadRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "CPU Main Thread Frame Time");
            yield return new WaitUntil(() => threadRecorder.Valid || millis - Environment.TickCount > 2000);

            if (threadRecorder.Valid)
            {
                float frameTimeMs = threadRecorder.LastValue / 1_000_000f;

                float targetFps = Application.targetFrameRate > 0 ? Application.targetFrameRate : 60f;
                float budgetFrameTimeMs = 1000f / targetFps;

                float rawCpuPercentage = Mathf.Min(frameTimeMs / budgetFrameTimeMs * 100f, 100f);

                yield return t.PrintDim($"  CPU load:    {Math.Round(rawCpuPercentage, 3)}%");
            } else yield return t.PrintDim($"  CPU load:    <color=#{HackerTerminal.ColorToHex(HackerTerminal.instance.normalColor)}>failed</color>");
            threadRecorder.Dispose();

            float totalSysMemMb = SystemInfo.systemMemorySize;
            long totalResMemBytes = Profiler.GetTotalReservedMemoryLong();

            long totalMonoMemBytes = GC.GetTotalMemory(false);

            double combinedAlloBytes = totalResMemBytes + totalMonoMemBytes;
            float alloMemMb = (float)(combinedAlloBytes / (1024f * 1024f));

            float ramUsagePercentage = Mathf.Clamp((alloMemMb / totalSysMemMb) * 100f, 0f, 100f);

            yield return t.PrintDim($"  RAM usage:   {Math.Round(ramUsagePercentage, 3)}%");
            yield return t.PrintDim($"  ICE layer:   {(UnityEngine.Random.value > 0.2f ? "ACTIVE" : "[!!] DEGRADED")}");
            yield return t.PrintDim($"  Ghost hops:  {UnityEngine.Random.Range(4, 12)}");
            yield return t.PrintDim($"  Packet loss: <color=#{HackerTerminal.ColorToHex(HackerTerminal.instance.normalColor)}>no data</color>");
            yield return t.PrintBlank();

        }
    }


    // reads output from terminal_fluff.json, add new ones in RegisterCommands() <3
    // line prefix !! = red, ! = amber, nothing = dim green
    [System.Serializable] public class FluffFile  { public List<FluffEntry> entries; }
    [System.Serializable] public class FluffEntry { public string key; public string[] lines; }
    public class FluffCommand : ITerminalCommand
    {
        public string Name        { get; }
        public string Description { get; }
        string _entryKey;
        public FluffCommand(string name, string entryKey, string description = "Run diagnostic routine.")
        {
            Name        = name;
            Description = description;
            _entryKey   = entryKey;
        }
        public IEnumerator Execute(HackerTerminal t, string[] args)
        {
            string[] lines = LoadLines();
            if (lines == null || lines.Length == 0)
            {
                yield return t.PrintError($"[{Name}] no data — check StreamingAssets/Terminal/terminal_fluff.json");
                yield break;
            }
            foreach (string line in lines)
            {
                if      (line.StartsWith("!!")) yield return t.PrintError  (line.Substring(2).TrimStart());
                else if (line.StartsWith("!"))  yield return t.PrintWarning(line.Substring(1).TrimStart());
                else                            yield return t.PrintDim    (line);
                yield return new WaitForSeconds(UnityEngine.Random.Range(0.03f, 0.12f));
            }
            yield return t.PrintBlank();
        }
        string[] LoadLines()
        {
            string path = System.IO.Path.Combine(Application.streamingAssetsPath, "Terminal", "terminal_fluff.json");
            if (!System.IO.File.Exists(path)) { Debug.LogError($"[FluffCommand] not found: {path}"); return null; }
            FluffFile data = JsonUtility.FromJson<FluffFile>(System.IO.File.ReadAllText(path));
            if (data == null || data.entries == null) return null;
            foreach (FluffEntry entry in data.entries)
                if (entry.key == _entryKey) return entry.lines;
            Debug.LogError($"[FluffCommand] key '{_entryKey}' not found in terminal_fluff.json");
            return null;
        }
    }

} 