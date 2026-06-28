using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

namespace CyberpunkTerminal
{
    public class HackerTerminal : MonoBehaviour
    {

        public static HackerTerminal instance = null;

//god i love headers and tooltips
        [Header("UI References")]
        public TMP_Text       terminalDisplay;
        public TMP_InputField hiddenInput;
        public ScrollRect scrollRect;
        [Header("Colors")]
        public Color normalColor  = new Color(0.18f, 1f,    0.38f);
        public Color dimColor     = new Color(0.10f, 0.55f, 0.22f);
        public Color warningColor = new Color(1f,    0.85f, 0.1f);
        public Color errorColor   = new Color(1f,    0.22f, 0.22f);
        [Header("Timing")]
        [Range(0.005f, 0.1f)] public float defaultCharDelay = 0.025f;
        [Range(0.005f, 0.1f)] public float fastCharDelay    = 0.008f;
        [Header("Events")]
        public UnityEvent<string, string> onLoginComplete;
        [Header("Debug")]
        [Tooltip("Press this key during the boot sequence to skip straight to the terminal.")]
        public Key debugSkipKey = Key.Space;
        public string  debugUsername = "RUNNER";
        public string  debugRoomCode = "DEBUG-01";



//private State
        private List<string> _lines        = new List<string>();
        private string       _currentInput = "";
        private bool         _acceptInput  = false;
        private bool         _cursorVisible = true;
        private string       _promptText   = "";
        private Dictionary<string, ITerminalCommand> _commands = new Dictionary<string, ITerminalCommand>();
        private bool _skipIntro = false;
        private string _username  = "";
        private string _roomCode  = "";
        private enum LoginStep { Username, RoomCode, Done }
        private LoginStep _loginStep = LoginStep.Username;
        public string Username => _username;
        public string RoomCode => _roomCode;
        private void Start()
        {
            
            if(instance == null)
                instance = this;
            else
                Destroy(gameObject);

            RegisterCommands();
            StartCoroutine(BlinkCursor());
            StartCoroutine(RunIntro());
        }
        static readonly (Key key, char normal, char shifted)[] _charKeys = {
            (Key.A,'a','A'),(Key.B,'b','B'),(Key.C,'c','C'),(Key.D,'d','D'),(Key.E,'e','E'),
            (Key.F,'f','F'),(Key.G,'g','G'),(Key.H,'h','H'),(Key.I,'i','I'),(Key.J,'j','J'),
            (Key.K,'k','K'),(Key.L,'l','L'),(Key.M,'m','M'),(Key.N,'n','N'),(Key.O,'o','O'),
            (Key.P,'p','P'),(Key.Q,'q','Q'),(Key.R,'r','R'),(Key.S,'s','S'),(Key.T,'t','T'),
            (Key.U,'u','U'),(Key.V,'v','V'),(Key.W,'w','W'),(Key.X,'x','X'),(Key.Y,'y','Y'),
            (Key.Z,'z','Z'),(Key.Digit0,'0',')'),(Key.Digit1,'1','!'),(Key.Digit2,'2','@'),
            (Key.Digit3,'3','#'),(Key.Digit4,'4','$'),(Key.Digit5,'5','%'),(Key.Digit6,'6','^'),
            (Key.Digit7,'7','&'),(Key.Digit8,'8','*'),(Key.Digit9,'9','('),
            (Key.Minus,'-','_'),(Key.Equals,'=','+'),(Key.LeftBracket,'[','{'),(Key.RightBracket,']','}'),
            (Key.Semicolon,';',':'),(Key.Quote,'\'','"'),(Key.Comma,',','<'),(Key.Period,'.','>'),(Key.Slash,'/','?'),
            (Key.Space,' ',' '),(Key.Numpad0,'0','0'),(Key.Numpad1,'1','1'),(Key.Numpad2,'2','2'),
            (Key.Numpad3,'3','3'),(Key.Numpad4,'4','4'),(Key.Numpad5,'5','5'),(Key.Numpad6,'6','6'),
            (Key.Numpad7,'7','7'),(Key.Numpad8,'8','8'),(Key.Numpad9,'9','9'),
        };
        private void Update()
        {
            if (Keyboard.current[debugSkipKey].wasPressedThisFrame && _loginStep != LoginStep.Done)
                _skipIntro = true;
            if (!_acceptInput) return;
            bool shift = Keyboard.current.shiftKey.isPressed;
            if (Keyboard.current.backspaceKey.wasPressedThisFrame && _currentInput.Length > 0)
            {
                _currentInput = _currentInput.Substring(0, _currentInput.Length - 1);
                RedrawDisplay();
                return;
            }
            if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
            {
                OnSubmit(_currentInput);
                return;
            }
            foreach (var (key, normal, shifted) in _charKeys)
            {
                if (Keyboard.current[key].wasPressedThisFrame)
                {
                    if (key == debugSkipKey) break;
                    _currentInput += shift ? shifted : normal;
                    RedrawDisplay();
                    break;
                }
            }
        }





//command registry
        private void RegisterCommands()
        {
            Register(new PingCommand());
            Register(new ScanCommand());
            Register(new WhoAmICommand());
            Register(new StatusCommand());
            Register(new FluffCommand("trace",   "trace",   "Trace route through ghost-net."));
            Register(new FluffCommand("decrypt", "decrypt", "Decrypt an intercepted payload."));
            Register(new FluffCommand("probe",   "probe",   "Probe target node for open ports."));
            //put new commands here mrrp :3
        }
        private void Register(ITerminalCommand cmd) => _commands[cmd.Name.ToLower()] = cmd;



//Intro sequence
        private IEnumerator RunIntro()
        {
            hiddenInput.interactable = false;
            string[] bootLines =
            {
                "NETRUNNER OS v9.1.4 // BUILD 20XX-BLADE",
                "Copyright (c) YAMA CORPORATION. All rights reserved.",
                "",
                "Initialising kernel........................... [OK]",
                "Loading entropy pool from /dev/quantum........ [OK]",
                "Mounting encrypted filesystem................. [OK]",
                "",
                ">> Checking hardware manifest...",
                "   CPU: Hitachi Synapse-X 32-core @ 4.7THz    [VERIFIED]",
                "   RAM: 512TB ECC L-DRAM                      [VERIFIED]",
                "   NIC: Ghost-Net Adaptive Mesh Transceiver v3 [VERIFIED]",
                "   ICE: Black Ice layer 7 — DB rev2904         [UPDATED]",
                "",
                ">> Spawning daemon grid...",
                "   auth_broker        PID 0x00A1  [LISTENING]",
                "   session_proxy      PID 0x00A2  [LISTENING]",
                "   entropy_harvester  PID 0x00A3  [LISTENING]",
                "   memwipe_watchdog   PID 0x00A4  [STANDBY]",
                "",
                ">> Establishing neural handshake with NETSEC relay...",
                "   Quantum key exchange............[44/44 BITS AGREED]",
                "   TLS over mesh tunnel............[NEGOTIATED]",
                "   Zero-knowledge proof............[PASS]",
                "",
                ">> Scanning ambient RF signatures...",
                "   [!!] 3 rogue access points detected — routing around.",
                "   [!!] Passive surveillance node at 192.168.88.0 — spoofed.",
                "   [OK] Clean egress path locked.",
                "",
                ">> Running pre-auth integrity checks...",
                "   BIOS checksum:  0xDEAD4E01 == 0xDEAD4E01  [MATCH]",
                "   Bootloader sig: RSA-8192 verified          [MATCH]",
                "   Runtime hash:   blake3:9f4a...c71b         [MATCH]",
                "",
                ">> SESSION READY.",
                "═══════════════════════════════════════════════════",
                "   YAMA SECURE NODE // DO NOT PROCEED IF OBSERVED",
                "═══════════════════════════════════════════════════",
                "",
            };
            foreach (string line in bootLines)
            {
                if (_skipIntro) break;
                yield return StartCoroutine(TypeLine(line, dimColor, fastCharDelay));
                yield return new WaitForSeconds(Random.Range(0f, 0.04f));
            }
            if (_skipIntro)
            {
                yield return StartCoroutine(SkipToTerminal());
                yield break;
            }
            yield return new WaitForSeconds(0.3f);
            yield return StartCoroutine(TypeLine("IDENTITY REQUIRED.", normalColor, defaultCharDelay, 0.2f));
            yield return StartCoroutine(TypeLine("", normalColor, 0f));
            ShowPrompt("> ENTER RUNNER TAG: ");
        }



  //Debug menu
        private IEnumerator SkipToTerminal()
        {
            _username  = debugUsername;
            _roomCode  = debugRoomCode;
            _loginStep = LoginStep.Done;
            yield return StartCoroutine(TypeLine("// DEBUG MODE — boot sequence skipped", warningColor, fastCharDelay));
            yield return StartCoroutine(TypeLine($"// Session: {_username} @ {_roomCode}", warningColor, fastCharDelay));
            yield return StartCoroutine(TypeLine("", normalColor, 0f));
            onLoginComplete?.Invoke(_username, _roomCode);
            ShowPrompt($"{_username}@yama:~$ ");
        }



//login
        private IEnumerator HandleLoginInput(string value)
        {
            if (_loginStep == LoginStep.Username)
            {
                _username  = value;
                _loginStep = LoginStep.RoomCode;
                CommitInputLine($"> ENTER RUNNER TAG: {_username}");
                yield return StartCoroutine(TypeLine($"RUNNER TAG [{_username}] — LOGGED.",            dimColor, fastCharDelay, 0.15f));
                yield return StartCoroutine(TypeLine("Cross-referencing ghost registry...",             dimColor, fastCharDelay));
                yield return StartCoroutine(TypeLine("   Alias not flagged in NETSEC blacklist.  [CLEAR]", dimColor, fastCharDelay));
                yield return StartCoroutine(TypeLine("   Social credit shadow score: NOMINAL.    [OK]", dimColor, fastCharDelay));
                yield return StartCoroutine(TypeLine("", normalColor, 0f));
                ShowPrompt("> ROOM CIPHER: ");
            }
            else if (_loginStep == LoginStep.RoomCode)
            {
                _roomCode  = value;
                _loginStep = LoginStep.Done;
                CommitInputLine($"> ROOM CIPHER: {_roomCode}");
                yield return StartCoroutine(TypeLine($"CIPHER [{_roomCode}] — RESOLVING...",             dimColor, fastCharDelay, 0.15f));
                yield return StartCoroutine(TypeLine("   Decrypting session token...",                   dimColor, fastCharDelay));
                yield return StartCoroutine(TypeLine("   Validating zero-knowledge proof...",            dimColor, fastCharDelay));
                yield return StartCoroutine(TypeLine("   Route obfuscation: 7 hops through ghost-net.", dimColor, fastCharDelay));
                yield return StartCoroutine(TypeLine("", normalColor, 0f));
                yield return StartCoroutine(TypeLine("╔══════════════════════════════╗", warningColor, fastCharDelay));
                yield return StartCoroutine(TypeLine("║      ACCESS GRANTED          ║", warningColor, defaultCharDelay));
                yield return StartCoroutine(TypeLine("╚══════════════════════════════╝", warningColor, fastCharDelay));
                yield return StartCoroutine(TypeLine("", normalColor, 0f));
                yield return StartCoroutine(TypeLine($"Welcome back, {_username}.", normalColor, defaultCharDelay));
                yield return StartCoroutine(TypeLine("Type 'help' for available commands.", dimColor, fastCharDelay));
                yield return StartCoroutine(TypeLine("", normalColor, 0f));
                yield return new WaitForSeconds(0.4f);
                onLoginComplete?.Invoke(_username, _roomCode);
                ShowPrompt($"{_username}@yama:~$ ");
            }
        }



//Inputs
        private void OnInputChanged(string value) { _currentInput = value; RedrawDisplay(); }

        private void OnSubmit(string value)
        {
            if (!_acceptInput) return;
            string trimmed   = value.Trim();
            hiddenInput.text = "";
            _currentInput    = "";
            _acceptInput     = false;
            if (string.IsNullOrEmpty(trimmed))
            {
                CommitInputLine(_promptText);
                ShowPrompt(_promptText);
                return;
            }
            StartCoroutine(_loginStep != LoginStep.Done
                ? HandleLoginInput(trimmed)
                : HandleCommand(trimmed));
        }




//commands

        private IEnumerator HandleCommand(string raw)
        {
            string[] parts   = raw.Split(' ');
            string   cmdName = parts[0].ToLower();
            CommitInputLine(_promptText + raw);

            if (cmdName == "help")
            {
                yield return StartCoroutine(TypeLine("Available commands:", normalColor, fastCharDelay));
                foreach (var kv in _commands)
                    yield return StartCoroutine(TypeLine($"  {kv.Key,-12} {kv.Value.Description}", dimColor, fastCharDelay));
                yield return StartCoroutine(TypeLine("", normalColor, 0f));
            }
            else if (_commands.TryGetValue(cmdName, out ITerminalCommand cmd))
            {
                yield return StartCoroutine(cmd.Execute(this, parts));
            }
            else
            {
                yield return StartCoroutine(TypeLine($"Command not found: {cmdName}", errorColor, fastCharDelay));
                yield return StartCoroutine(TypeLine("Type 'help' for available commands.", dimColor, fastCharDelay));
                yield return StartCoroutine(TypeLine("", normalColor, 0f));
            }

            ShowPrompt(_promptText);
        }



//Prompt
        private void ShowPrompt(string prompt)
        {
            _promptText  = prompt;
            _acceptInput = true;
            hiddenInput.interactable = true;
            hiddenInput.ActivateInputField();
            RedrawDisplay();
        }
        private void CommitInputLine(string line) => _lines.Add(WrapColor(line, normalColor));




//print
        public IEnumerator Print(string text, Color color, float charDelay = -1f)
            => TypeLine(text, color, charDelay < 0 ? fastCharDelay : charDelay);
        public IEnumerator PrintNormal (string text) => Print(text, normalColor);
        public IEnumerator PrintDim    (string text) => Print(text, dimColor);
        public IEnumerator PrintWarning(string text) => Print(text, warningColor);
        public IEnumerator PrintError  (string text) => Print(text, errorColor);
        public IEnumerator PrintBlank  ()            => Print("",   normalColor, 0f);



//typing effect
        private IEnumerator TypeLine(string text, Color color, float charDelay, float preDelay = 0f)
        {
            if (preDelay > 0f) yield return new WaitForSeconds(preDelay);
            string hex   = ColorToHex(color);
            int    idx   = _lines.Count;
            string built = "";
            _lines.Add("");
            foreach (char c in text)
            {
                built       += c;
                _lines[idx]  = $"<color=#{hex}>{built}</color>";
                if (charDelay > 0f) { RedrawDisplay(idx); yield return new WaitForSeconds(charDelay); }
            }
            _lines[idx] = $"<color=#{hex}>{text}</color>";
            RedrawDisplay();
        }



//display 
        private void RedrawDisplay(int activeIdx = -1)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < _lines.Count; i++)
            {
                string suffix = (i == activeIdx && _cursorVisible) ? WrapColor("█", normalColor) : "";
                sb.AppendLine(_lines[i] + suffix);
            }
            if (_acceptInput)
                sb.AppendLine(WrapColor(_promptText + _currentInput + (_cursorVisible ? "█" : " "), normalColor));
            terminalDisplay.text = sb.ToString();
            terminalDisplay.ForceMeshUpdate();
            float h = terminalDisplay.preferredHeight;
            terminalDisplay.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
            if (scrollRect != null) scrollRect.verticalNormalizedPosition = 0f;
        }



//cursor blink
        private IEnumerator BlinkCursor()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.53f);
                _cursorVisible = !_cursorVisible;
                RedrawDisplay();
            }
        }


//utils
        private string WrapColor(string text, Color color) => $"<color=#{ColorToHex(color)}>{text}</color>";
        public static string ColorToHex(Color c) => UnityEngine.ColorUtility.ToHtmlStringRGB(c);
    }
}