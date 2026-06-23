using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace CyberpunkTerminal
{
    public class HackerTerminal : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("The main TMP text box that shows the scrolling terminal output.")]
        public TMP_Text terminalDisplay;

        [Tooltip("Invisible TMP_InputField for capturing keystrokes. Stretch it over " +
                 "the whole screen and set its alpha to 0 — it still receives input.")]
        public TMP_InputField hiddenInput;

        [Tooltip("The ScrollRect wrapping the terminal text. Required for auto-scroll.")]
        public ScrollRect scrollRect;

        [Tooltip("A blinking cursor character appended to the current input line.")]
        public string cursorChar = "█";

        [Header("Colors")]
        public Color normalColor  = new Color(0.18f, 1f, 0.38f);    // phosphor green
        public Color dimColor     = new Color(0.10f, 0.55f, 0.22f); // dimmer green
        public Color warningColor = new Color(1f, 0.85f, 0.1f);     // amber
        public Color errorColor   = new Color(1f, 0.22f, 0.22f);    // red

        [Header("Timing")]
        [Range(0.01f, 0.1f)]
        public float defaultCharDelay = 0.025f;
        [Range(0.01f, 0.1f)]
        public float junkCharDelay    = 0.01f; 

        [Header("Events")]
        [Tooltip("Fired when the player has entered both username and room code. " +
                 "String args: username, roomCode")]
        public UnityEvent<string, string> onLoginComplete;


        private List<string> _displayLines   = new List<string>();
        private string       _currentInput   = "";
        private bool         _awaitingUsername = false;
        private bool         _awaitingRoomCode = false;
        private string       _username       = "";
        private string       _roomCode       = "";
        private bool         _cursorVisible  = true;
        private Coroutine    _cursorBlink;


        private readonly string[] _bootLines = new string[]
        {
            "NETRUNNER OS v9.1.4 // BUILD 20XX-BLADE",
            "Copyright (c) YAMA CORPORATION. All rights reserved.",
            "",
            "Initialising kernel... [OK]",
            "Loading entropy pool from /dev/quantum... [OK]",
            "Mounting encrypted filesystem... [OK]",
            "",
            ">> Checking hardware manifest...",
            "   CPU: Hitachi Synapse-X 32-core @ 4.7THz          [VERIFIED]",
            "   RAM: 512TB ECC L-DRAM                            [VERIFIED]",
            "   NIC: Ghost-Net Adaptive Mesh Transceiver v3       [VERIFIED]",
            "   ICE: Black Ice layer 7 — threat signature DB rev2904 [UPDATED]",
            "",
            ">> Spawning daemon grid...",
            "   auth_broker           PID 0x00A1   [LISTENING]",
            "   session_proxy         PID 0x00A2   [LISTENING]",
            "   entropy_harvester     PID 0x00A3   [LISTENING]",
            "   memwipe_watchdog      PID 0x00A4   [STANDBY]",
            "",
            ">> Establishing neural handshake with NETSEC relay...",
            "   Quantum key exchange............... [44/44 BITS AGREED]",
            "   TLS over mesh tunnel................[NEGOTIATED]",
            "   Zero-knowledge proof verified....... [PASS]",
            "",
            ">> Scanning ambient RF signatures...",
            "   [!!] 3 rogue access points detected — routing around them.",
            "   [!!] Passive surveillance node at 192.168.88.0 — spoofed.",
            "   [OK] Clean egress path locked.",
            "",
            ">> Running pre-auth integrity checks...",
            "   BIOS checksum:  0xDEAD4E01  == 0xDEAD4E01   [MATCH]",
            "   Bootloader sig: RSA-8192 verified             [MATCH]",
            "   Runtime hash:   blake3:9f4a...c71b            [MATCH]",
            "",
            ">> Loading personality matrix...",
            "   Tone:     COLD",
            "   Patience: MINIMAL",
            "   Trust:    PROVISIONAL",
            "",
            ">> SESSION READY.",
            "═══════════════════════════════════════════════════════════",
            "   YAMA SECURE NODE // DO NOT PROCEED IF OBSERVED",
            "═══════════════════════════════════════════════════════════",
            "",
        };


        private void Start()
        {
            hiddenInput.onValueChanged.AddListener(OnInputChanged);
            hiddenInput.onSubmit.AddListener(OnInputSubmitted);

            StartCoroutine(RunBootSequence());
            _cursorBlink = StartCoroutine(BlinkCursor());
        }

        private void Update()
        {

            if (!hiddenInput.isFocused)
                hiddenInput.ActivateInputField();
        }


        private IEnumerator RunBootSequence()
        {
            hiddenInput.interactable = false;

            foreach (string line in _bootLines)
            {
                yield return StartCoroutine(TypeLine(line, dimColor, junkCharDelay));
                yield return new WaitForSeconds(Random.Range(0f, 0.05f));
            }

            yield return new WaitForSeconds(0.4f);
            yield return StartCoroutine(TypeLine("IDENTITY REQUIRED.", normalColor, defaultCharDelay, 0.3f));
            yield return StartCoroutine(TypeLine("", normalColor, 0));
            yield return StartCoroutine(TypeLine("> ENTER RUNNER TAG:", normalColor, defaultCharDelay));

            _awaitingUsername = true;
            hiddenInput.interactable = true;
            hiddenInput.ActivateInputField();
            RedrawDisplay();
        }

        private void OnInputChanged(string value)
        {
            _currentInput = value;
            RedrawDisplay();
        }

        private void OnInputSubmitted(string value)
        {
            string trimmed = value.Trim();
            hiddenInput.text = "";
            _currentInput = "";

            if (_awaitingUsername)
            {
                if (string.IsNullOrEmpty(trimmed)) return;

                _username = trimmed;
                _awaitingUsername = false;

                _displayLines.Add(WrapColor($"> ENTER RUNNER TAG: {_username}", normalColor));
                _displayLines.Add(WrapColor("", normalColor));

                StartCoroutine(PromptForRoomCode());
            }
            else if (_awaitingRoomCode)
            {
                if (string.IsNullOrEmpty(trimmed)) return;

                _roomCode = trimmed;
                _awaitingRoomCode = false;

                _displayLines.Add(WrapColor($"> ROOM CIPHER: {_roomCode}", normalColor));
                _displayLines.Add(WrapColor("", normalColor));

                StartCoroutine(FinishLogin());
            }

            RedrawDisplay();
        }


        private IEnumerator PromptForRoomCode()
        {
            hiddenInput.interactable = false;

            yield return StartCoroutine(TypeLine($"RUNNER TAG [{_username}] — LOGGED.", dimColor, junkCharDelay, 0.2f));
            yield return StartCoroutine(TypeLine("Cross-referencing with ghost registry...", dimColor, junkCharDelay));
            yield return StartCoroutine(TypeLine("   Alias not flagged in NETSEC blacklist.  [CLEAR]", dimColor, junkCharDelay));
            yield return StartCoroutine(TypeLine("   Social credit shadow score: NOMINAL.    [OK]", dimColor, junkCharDelay));
            yield return StartCoroutine(TypeLine("", normalColor, 0));
            yield return StartCoroutine(TypeLine("> ROOM CIPHER:", normalColor, defaultCharDelay));

            _awaitingRoomCode = true;
            hiddenInput.interactable = true;
            hiddenInput.ActivateInputField();
            RedrawDisplay();
        }


        private IEnumerator FinishLogin()
        {
            hiddenInput.interactable = false;

            yield return StartCoroutine(TypeLine($"CIPHER [{_roomCode}] — RESOLVING...", dimColor, junkCharDelay, 0.2f));
            yield return StartCoroutine(TypeLine("   Decrypting session token...", dimColor, junkCharDelay));
            yield return StartCoroutine(TypeLine("   Validating zero-knowledge proof...", dimColor, junkCharDelay));
            yield return StartCoroutine(TypeLine("   Route obfuscation: 7 hops through ghost-net.", dimColor, junkCharDelay));
            yield return StartCoroutine(TypeLine("", normalColor, 0));
            yield return StartCoroutine(TypeLine("╔══════════════════════════════╗", warningColor, junkCharDelay));
            yield return StartCoroutine(TypeLine("║     ACCESS GRANTED           ║", warningColor, defaultCharDelay));
            yield return StartCoroutine(TypeLine("╚══════════════════════════════╝", warningColor, junkCharDelay));
            yield return StartCoroutine(TypeLine("", normalColor, 0));
            yield return StartCoroutine(TypeLine($"Welcome back, {_username}.", normalColor, defaultCharDelay));
            yield return StartCoroutine(TypeLine("Connecting to node... stand by.", dimColor, junkCharDelay));
            yield return new WaitForSeconds(0.8f);

            if (_cursorBlink != null)
                StopCoroutine(_cursorBlink);

            onLoginComplete?.Invoke(_username, _roomCode);
        }


        private IEnumerator TypeLine(string text, Color color, float charDelay, float preDelay = 0f)
        {
            if (preDelay > 0f)
                yield return new WaitForSeconds(preDelay);

            string built = "";
            string hex   = ColorToHex(color);

            int idx = _displayLines.Count;
            _displayLines.Add("");

            foreach (char c in text)
            {
                built += c;
                _displayLines[idx] = $"<color=#{hex}>{built}</color>";
                RedrawDisplay(idx);
                if (charDelay > 0f)
                    yield return new WaitForSeconds(charDelay);
            }

            _displayLines[idx] = $"<color=#{hex}>{text}</color>";
            RedrawDisplay();
        }

        private void RedrawDisplay(int activeLineIdx = -1)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            for (int i = 0; i < _displayLines.Count; i++)
            {
                if (i == activeLineIdx)
                    sb.AppendLine(_displayLines[i] + WrapColor(_cursorVisible ? cursorChar : " ", normalColor));
                else
                    sb.AppendLine(_displayLines[i]);
            }

            if (_awaitingUsername || _awaitingRoomCode)
            {
                string prompt = _awaitingUsername ? "> ENTER RUNNER TAG: " : "> ROOM CIPHER: ";
                string cursor = _cursorVisible ? cursorChar : " ";
                sb.AppendLine(WrapColor(prompt + _currentInput + cursor, normalColor));
            }

            terminalDisplay.text = sb.ToString();

            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private IEnumerator BlinkCursor()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.53f);
                _cursorVisible = !_cursorVisible;
                RedrawDisplay();
            }
        }

        private string WrapColor(string text, Color color)
            => $"<color=#{ColorToHex(color)}>{text}</color>";

        private string ColorToHex(Color c)
            => ColorUtility.ToHtmlStringRGB(c);
    }
}