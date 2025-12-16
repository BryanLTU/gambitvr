using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using XRMultiplayer;
using Unity.Netcode;

public class RuntimeConsole : MonoBehaviour
{
    [Header("UI Toolkit Assets")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Input System")]
    [SerializeField] private InputActionReference toggleAction;

    private InputAction _runtimeToggleAction;

    private VisualElement _root;
    private VisualElement _consoleRoot;
    private ScrollView _scroll;
    private VisualElement _content;
    private TextField _input;

    private readonly List<string> _history = new();
    private int _historyIndex = -1;

    private readonly Dictionary<string, Action<string[]>> _commands =
        new(StringComparer.OrdinalIgnoreCase);

    private void Awake()
    {
        if (!uiDocument) uiDocument = GetComponent<UIDocument>();
        if (!uiDocument)
        {
            Debug.LogError("RuntimeConsole requires a UIDocument.");
            enabled = false;
            return;
        }

        var panelRoot = uiDocument.rootVisualElement.panel.visualTree;
        _consoleRoot = panelRoot.Q<VisualElement>("console-root");
        _scroll      = panelRoot.Q<ScrollView>("log-scroll");
        _content     = panelRoot.Q("log-content");
        _input       = panelRoot.Q<TextField>("command-input");

        SetVisible(false);

        RegisterBuiltins();
        _input.RegisterCallback<KeyUpEvent>(OnInputKeyUp);
        _input.RegisterCallback<KeyDownEvent>(OnInputKeyDown);

        Application.logMessageReceived += OnUnityLog;

        SetupToggleAction();

        PrintSystem("Console ready. Type 'help'.");
    }

    private void OnEnable()
    {
        EnableToggleAction(true);
    }

    private void OnDisable()
    {
        EnableToggleAction(false);
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= OnUnityLog;

        if (_runtimeToggleAction != null)
        {
            _runtimeToggleAction.performed -= OnTogglePerformed;
            _runtimeToggleAction.Dispose();
            _runtimeToggleAction = null;
        }
    }

    private void SetupToggleAction()
    {
        if (toggleAction != null && toggleAction.action != null)
        {
            toggleAction.action.performed -= OnTogglePerformed;
            toggleAction.action.performed += OnTogglePerformed;
            return;
        }

        _runtimeToggleAction.performed += OnTogglePerformed;
    }

    private void EnableToggleAction(bool enabled)
    {
        if (toggleAction != null && toggleAction.action != null)
        {
            if (enabled) toggleAction.action.Enable();
            else toggleAction.action.Disable();
            return;
        }

        if (_runtimeToggleAction != null)
        {
            if (enabled) _runtimeToggleAction.Enable();
            else _runtimeToggleAction.Disable();
        }
    }

    private void OnTogglePerformed(InputAction.CallbackContext _)
    {
        bool willShow = _consoleRoot.resolvedStyle.display == DisplayStyle.None;
        SetVisible(willShow);
    }

    private void SetVisible(bool visible)
    {
        _consoleRoot.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        if (visible)
        {
            _input.Focus();
            _input.SelectAll();
        }
    }

    private void OnUnityLog(string condition, string stackTrace, LogType type)
    {
        switch (type)
        {
            case LogType.Warning:
                Print(condition, "warn");
                break;
            case LogType.Error:
            case LogType.Exception:
            case LogType.Assert:
                Print(condition, "err");
                if (!string.IsNullOrEmpty(stackTrace))
                    Print(stackTrace, "err");
                break;
            default:
                Print(condition, "info");
                break;
        }
    }

    private void OnInputKeyUp(KeyUpEvent evt)
    {
        if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter)
            return;

        var text = _input.value?.Trim();
        if (!string.IsNullOrEmpty(text))
        {
            Run(text);
            _history.Add(text);
            _historyIndex = _history.Count;
        }

        _input.value = string.Empty;
        _input.Focus();

        evt.StopPropagation();
    }

    private void OnInputKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.UpArrow)
        {
            if (_history.Count == 0) return;
            _historyIndex = Mathf.Clamp(_historyIndex - 1, 0, _history.Count - 1);
            _input.value = _history[_historyIndex];
            _input.SelectAll();
            evt.StopPropagation();
            return;
        }

        if (evt.keyCode == KeyCode.DownArrow)
        {
            if (_history.Count == 0) return;
            _historyIndex = Mathf.Clamp(_historyIndex + 1, 0, _history.Count);
            _input.value = (_historyIndex >= _history.Count) ? string.Empty : _history[_historyIndex];
            _input.SelectAll();
            evt.StopPropagation();
            return;
        }
    }

    private void Run(string line)
    {
        Print($"> {line}", "cmd");

        var parts = SplitArgs(line);
        if (parts.Length == 0) return;

        var cmd = parts[0];
        var args = parts.Skip(1).ToArray();

        if (_commands.TryGetValue(cmd, out var action))
        {
            try { action.Invoke(args); }
            catch (Exception ex) { Print(ex.ToString(), "err"); }
        }
        else
        {
            Print($"Unknown command '{cmd}'. Type 'help'.", "warn");
        }
    }

    private void Register(string name, Action<string[]> handler) => _commands[name] = handler;

    private void RegisterBuiltins()
    {
        Register("help", _ =>
        {
            PrintSystem("Commands:");
            foreach (var k in _commands.Keys.OrderBy(x => x))
                PrintSystem($"- {k}");
        });

        Register("clear", _ => _content.Clear());
        Register("echo", args => Print(string.Join(" ", args), "info"));

        Register("quickjoin", _ =>
        {
            var mgr = XRINetworkGameManager.Instance;
            if (!mgr)
            {
                Print("XRINetworkGameManager not found", "err");
                return;
            }

            mgr.QuickJoinLobby();
            PrintSystem("QuickJoinLobby invoked");
        });

        Register("moverandom", _ =>
        {
            MoveRandomMyPiece();
        });

        Register("forceduel", _ =>
        {
            ForceDuel();
        });
    }

    private void PrintSystem(string msg) => Print(msg, "sys");

    private void Print(string msg, string className)
    {
        var label = new Label(msg);
        label.AddToClassList("console-line");
        label.AddToClassList(className);
        _content.Add(label);

        _scroll.schedule.Execute(() =>
        {
            _scroll.scrollOffset = new Vector2(0, float.MaxValue);
        }).StartingIn(1);
    }

    private static string[] SplitArgs(string input)
    {
        var args = new List<string>();
        var current = "";
        bool inQuotes = false;

        foreach (var c in input)
        {
            if (c == '"') { inQuotes = !inQuotes; continue; }

            if (!inQuotes && char.IsWhiteSpace(c))
            {
                if (current.Length > 0) { args.Add(current); current = ""; }
            }
            else current += c;
        }

        if (current.Length > 0) args.Add(current);
        return args.ToArray();
    }

    private void MoveRandomMyPiece()
    {
        if (!ChessGame.Instance || !ChessGameNet.Instance)
        {
            Print("ChessGame or ChessGameNet missing", "err");
            return;
        }

        if (!ChessGameNet.Instance.TryGetLocalPlayerColor(out var myColor))
        {
            Print("Local player has no assigned color yet", "err");
            return;
        }

        if (ChessGame.Instance.currentTurn != myColor)
        {
            Print($"Not your turn. Current turn: {ChessGame.Instance.currentTurn}", "warn");
            return;
        }

        var candidates = new List<(ChessPiece piece, BoardSquare square)>();

        var pieces = ChessGame.Instance.GetAllBoardPieces();
        foreach (var p in pieces)
        {
            if (!p || p.currentSquare == null) continue;
            if (!ChessGameNet.Instance.CanLocalPlayerControlPiece(p)) continue;
            if (p.pieceColor != ChessGame.Instance.currentTurn) continue;

            var legal = ChessGame.Instance.GetLegalMoves(p);
            foreach (var sq in legal)
            {
                candidates.Add((p, sq));
            }
        }

        if (candidates.Count == 0)
        {
            Print("No legal moves available", "err");
            return;
        }

        var (piece, target) = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        var nobj = piece.GetComponent<NetworkObject>();

        if (!nobj)
        {
            Print($"Piece {piece.name} has no NetworkObject", "err");
            return;
        }

        var occupant = ChessGame.Instance.GetPieceAt(target.file, target.rank);
        bool willDuel = occupant != null && occupant.pieceColor != piece.pieceColor;

        ChessGameNet.Instance.SubmitMoveRpc(nobj.NetworkObjectId, target.file, target.rank);

        if (willDuel)
        {
            PrintSystem($"RandomMove: {piece.pieceType} {piece.pieceColor} attacks {occupant.pieceType} at ({target.file},{target.rank}) | Duel");
        }
        else
        {
            PrintSystem($"RandomMove: {piece.pieceType} {piece.pieceColor} to ({target.file},{target.rank})");
        }
    }

    private void ForceDuel()
    {
        if (!ChessGame.Instance || !ChessGameNet.Instance)
        {
            Print("ChessGame/ChessGameNet missing", "err");
            return;
        }

        ulong localId = NetworkManager.Singleton.LocalClientId;

        var myPieces = FindObjectsByType<ChessPiece>(FindObjectsSortMode.None)
            .Where(p => p && p.currentSquare != null && ChessGameNet.Instance.CanClientControlPiece(localId, p))
            .ToList();
        var enemyPieces = FindObjectsByType<ChessPiece>(FindObjectsSortMode.None)
            .Where(p => p && p.currentSquare != null && !ChessGameNet.Instance.CanClientControlPiece(localId, p))
            .ToList();

        if (myPieces.Count == 0 || enemyPieces.Count == 0)
        {
            Print("Missing attacker / defender pieces", "warn");
            return;
        }

        var attacker = myPieces[UnityEngine.Random.Range(0, myPieces.Count)];
        var defender = enemyPieces[UnityEngine.Random.Range(0, enemyPieces.Count)];

        var aN = attacker.GetComponent<NetworkObject>();
        var dN = defender.GetComponent<NetworkObject>();
        if (!aN || !dN)
        {
            Print("Attacker/defender missing NetworkObject", "err");
            return;
        }

        int tf = defender.currentSquare.file;
        int tr = defender.currentSquare.rank;

        ChessGameNet.Instance.ForceDuelRpc(aN.NetworkObjectId, dN.NetworkObjectId, tf, tr);

        Print($"ForceDuel: {attacker.pieceType} vs {defender.pieceType} at ({tf},{tr})", "sys");
    }
}
