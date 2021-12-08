using System;
using System.Collections;
using System.Collections.Generic;
using KModkit;
using Mayhem;
using UnityEngine;

using Rnd = UnityEngine.Random;

public class MayhemScript : MonoBehaviour
{
    [UnityEditor.MenuItem("DoStuff/DoStuff")]
    public static void DoStuff()
    {
        var m = FindObjectOfType<MayhemScript>();
        var template = m.transform.Find("Hexagons").Find("Hex1Parent").Find("Hex1").Find("Collider1").gameObject;
        for (var i = 2; i <= 19; i++)
        {
            var hex = m.transform.Find("Hexagons").Find("Hex" + i + "Parent").Find("Hex" + i);
            var coll = Instantiate(template, hex.transform);
            coll.name = "Collider" + i;
            hex.GetComponent<KMSelectable>().SelectableColliders = new[] { coll.GetComponent<Collider>() };
            DestroyImmediate(hex.GetComponent<Collider>());
        }
    }

    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMSelectable[] HexSelectables;
    public GameObject[] Hexes, HexFronts, HexBacks;
    public GameObject StatusLightObj, HexesParent;
    public Material[] HexBlueMats;
    public Material LightBlueMat, StartingHexMat, HexRedMat, HexBlackMat;
    public Light[] HexLights;
    public TextMesh ShowOffText;

    private static int _moduleIdCounter = 1;
    private int _moduleId, _startingHex, _currentHex = 99, _highlightedHex = 99;
    private readonly int[] _correctHexes = new int[7];
    private bool _moduleSolved, _areHexesRed, _areHexesFlashing, _canStagesContinue, _areHexesBlack, _showOff, _firstFlash = true;
    private readonly bool[] _isHexHighlighted = new bool[19];
    private string SerialNumber;
    private static readonly string[] sounds = { "Flash1", "Flash2", "Flash3", "Flash4", "Flash5", "Flash6", "Flash7" };
    private static readonly string[] POS = { "first", "second", "third", "fourth", "fifth", "sixth", "seventh" };
    private static readonly float[] xPos = {
        -0.052f, -0.052f, -0.052f,
        -0.026f, -0.026f, -0.026f, -0.026f,
        0f, 0f, 0f, 0f, 0f,
        0.026f, 0.026f, 0.026f, 0.026f,
        0.052f, 0.052f, 0.052f };
    private static readonly float[] zPos = {
        0.03f, 0f, -0.03f,
        0.045f, 0.015f, -0.015f, -0.045f,
        0.06f, 0.03f, 0f, -0.03f, -0.06f,
        0.045f, 0.015f, -0.015f, -0.045f,
        0.03f, 0f, -0.03f };

    public class MayhemSettings
    {
        public bool UseCopyrightedMusic;
    }

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        SetHexMaterials();
        PickStartingHex();
        float scalar = transform.lossyScale.x;
        foreach (Light light in HexLights)
            light.range *= scalar;
        for (int i = 0; i < HexSelectables.Length; i++)
        {
            int j = i;
            HexSelectables[i].OnHighlight += delegate ()
            {
                if (!_moduleSolved)
                    HexHighlight(j);
            };
            HexSelectables[i].OnHighlightEnded += delegate ()
            {
                if (!_moduleSolved)
                    HexHighlightEnd(j);
            };
            HexSelectables[i].OnInteract += delegate ()
            {
                if (!_areHexesFlashing && !_moduleSolved)
                    HexPress(j);
                return false;
            };
            HexSelectables[i].OnInteractEnded += delegate ()
            {
                if (!_moduleSolved)
                    HexRelease(j);
            };
        }
        SerialNumber = BombInfo.GetSerialNumber();
        DecideCorrectHexes();
        Invoke("DoSettings", 0.1f);
    }

    private void DecideCorrectHexes()
    {
        int temp = 0;
        for (int i = 0; i < 7; i++)
        {
            if (i != 0)
                temp += SerialNumber[i - 1] >= '0' && SerialNumber[i - 1] <= '9' ? SerialNumber[i - 1] - '0' : SerialNumber[i - 1] - 'A' + 1;
            _correctHexes[i] = (_startingHex + temp) % 19;
        }
        Debug.LogFormat("[Mayhem #{0}] Starting hex is at position {1}.", _moduleId, _correctHexes[0] + 1);
        int tempTwo = _correctHexes[0] + 1;
        for (int i = 0; i < 6; i++)
        {
            if (tempTwo > 19)
                tempTwo -= 19;
            Debug.LogFormat("[Mayhem #{0}] The {1} character of the serial number is {2} ({3}). Adding this to {4} gets you the {5} hex at position {6}.",
                _moduleId, POS[i], SerialNumber[i],
                SerialNumber[i] >= '0' && SerialNumber[i] <= '9' ? SerialNumber[i] - '0' : SerialNumber[i] - 'A' + 1,
                tempTwo, POS[i + 1], _correctHexes[i + 1] + 1
                );
            tempTwo += SerialNumber[i] >= '0' && SerialNumber[i] <= '9' ? SerialNumber[i] - '0' : SerialNumber[i] - 'A' + 1;
        }
    }

    private void SetHexMaterials()
    {
        for (int i = 0; i < HexFronts.Length; i++)
        {
            if (!_isHexHighlighted[i])
            {
                HexFronts[i].GetComponent<MeshRenderer>().material = HexBlueMats[i];
                HexBacks[i].GetComponent<MeshRenderer>().material = HexBlueMats[i];
            }
        }
    }

    private void SetHexesBlack()
    {
        for (int i = 0; i < HexFronts.Length; i++)
        {
            if (!_isHexHighlighted[i])
            {
                HexFronts[i].GetComponent<MeshRenderer>().material = HexBlackMat;
                HexBacks[i].GetComponent<MeshRenderer>().material = HexBlackMat;
            }
        }
    }
    private void PickStartingHex()
    {
        _startingHex = Rnd.Range(0, 19);
        HexFronts[_startingHex].GetComponent<MeshRenderer>().material = StartingHexMat;
        HexFronts[_startingHex].GetComponent<MeshRenderer>().material = StartingHexMat;
    }
    private void HexHighlight(int j)
    {
        HexFronts[j].GetComponent<MeshRenderer>().material = LightBlueMat;
        HexBacks[j].GetComponent<MeshRenderer>().material = LightBlueMat;
        _isHexHighlighted[j] = true;
        _highlightedHex = j;
    }

    private void HexHighlightEnd(int j)
    {
        if (_areHexesRed)
        {
            if (j != _currentHex)
            {
                HexFronts[j].GetComponent<MeshRenderer>().material = HexRedMat;
                HexBacks[j].GetComponent<MeshRenderer>().material = HexRedMat;
            }
            else if (j == _startingHex && _firstFlash)
            {
                HexFronts[j].GetComponent<MeshRenderer>().material = StartingHexMat;
                HexBacks[j].GetComponent<MeshRenderer>().material = StartingHexMat;
            }
            else
            {
                HexFronts[j].GetComponent<MeshRenderer>().material = HexBlueMats[j];
                HexBacks[j].GetComponent<MeshRenderer>().material = HexBlueMats[j];
            }
        }
        else if (j == _startingHex && _firstFlash)
        {
            HexFronts[j].GetComponent<MeshRenderer>().material = StartingHexMat;
            HexBacks[j].GetComponent<MeshRenderer>().material = StartingHexMat;
        }
        else if (_areHexesBlack)
        {
            HexFronts[j].GetComponent<MeshRenderer>().material = HexBlackMat;
            HexBacks[j].GetComponent<MeshRenderer>().material = HexBlackMat;
        }
        else
        {
            HexFronts[j].GetComponent<MeshRenderer>().material = HexBlueMats[j];
            HexBacks[j].GetComponent<MeshRenderer>().material = HexBlueMats[j];
        }
        _isHexHighlighted[j] = false;
        _highlightedHex = 99;
    }

    private void HexPress(int h)
    {
        if (!_areHexesFlashing && !_areHexesBlack)
        {
            _canStagesContinue = true;
            StartCoroutine(FlashHexes());
            _areHexesFlashing = true;
        }
    }
    private void HexRelease(int h)
    {
        if (_isHexHighlighted[h])
            HexHighlight(h);
    }
    private IEnumerator FlashHexes()
    {
        yield return new WaitForSeconds(0.2f);
        for (int i = 0; i < _correctHexes.Length; i++)
        {
            Audio.PlaySoundAtTransform(sounds[i], transform);
            _currentHex = _correctHexes[i];
            yield return new WaitForSeconds(1.71f);
            if (i == 0)
            {
                SetHexMaterials();
                _firstFlash = false;
            }
            for (int j = 0; j < Hexes.Length; j++)
            {
                if (j != _correctHexes[i] && j != _highlightedHex)
                {
                    HexFronts[j].GetComponent<MeshRenderer>().material = HexRedMat;
                    HexBacks[j].GetComponent<MeshRenderer>().material = HexRedMat;
                }
            }
            _areHexesRed = true;
            yield return new WaitForSeconds(2.11f);
            _areHexesRed = false;
            for (int j = 0; j < Hexes.Length; j++)
            {
                if (j == _highlightedHex)
                {
                    HexFronts[j].GetComponent<MeshRenderer>().material = LightBlueMat;
                    HexBacks[j].GetComponent<MeshRenderer>().material = LightBlueMat;
                }
                else
                    SetHexMaterials();
            }
            if (!_canStagesContinue)
            {
                if (_showOff)
                {
                    StartCoroutine(ShowOffStrike());
                }
                else
                {
                    StartCoroutine(OpenHex(_currentHex, false));
                    StartCoroutine(MoveLight(_currentHex, false));
                }
                yield break;
            }
            _currentHex = 99;
            if (i == 6)
            {
                Audio.PlaySoundAtTransform("Solve", transform);
                _areHexesFlashing = false;
                _moduleSolved = true;
                StartCoroutine(OpenHex(_correctHexes[6], true));
                StartCoroutine(MoveLight(_correctHexes[6], true));
            }
        }
    }

    private IEnumerator OpenHex(int hex, bool isSolve)
    {
        var durationFirst = 0.3f;
        var elapsedFirst = 0f;
        float waitTime;
        if (isSolve)
            waitTime = 0.5f;
        else
            waitTime = 1f;
        Audio.PlaySoundAtTransform("HexOpen", transform);
        while (elapsedFirst < durationFirst)
        {
            Hexes[hex].transform.localEulerAngles = new Vector3(Easing.InOutQuad(elapsedFirst, 0f, 90f, durationFirst), 0f, 0f);
            yield return null;
            elapsedFirst += Time.deltaTime;
        }
        Hexes[hex].transform.localEulerAngles = new Vector3(90f, 0f, 0f);
        yield return new WaitForSeconds(waitTime);
        var durationSecond = 0.3f;
        var elapsedSecond = 0f;
        while (elapsedSecond < durationSecond)
        {
            Hexes[hex].transform.localEulerAngles = new Vector3(Easing.InOutQuad(elapsedSecond, 90f, 0f, durationSecond), 0f, 0f);
            yield return null;
            elapsedSecond += Time.deltaTime;
        }
        Audio.PlaySoundAtTransform("HexClose", transform);
        Hexes[hex].transform.localEulerAngles = new Vector3(0f, 0f, 0f);
    }

    private IEnumerator MoveLight(int j, bool isSolve)
    {
        yield return new WaitForSeconds(0.5f);
        Audio.PlaySoundAtTransform("Hooo", transform);
        var durationFirst = 0.3f;
        var elapsedFirst = 0f;
        while (elapsedFirst < durationFirst)
        {
            StatusLightObj.transform.localPosition = new Vector3(xPos[j], Easing.InOutQuad(elapsedFirst, -0.04f, 0.05f, durationFirst), zPos[j]);
            yield return null;
            elapsedFirst += Time.deltaTime;
        }
        StatusLightObj.transform.localPosition = new Vector3(xPos[j], 0.05f, zPos[j]);
        float durationSecond;
        float waitTime;
        float yPos;
        var elapsedSecond = 0f;
        if (isSolve)
        {
            waitTime = 0.1f;
            durationSecond = 0.2f;
            yPos = 0.02f;
            HexFronts[_correctHexes[6]].GetComponent<MeshRenderer>().material = LightBlueMat;
            HexFronts[_correctHexes[6]].GetComponent<MeshRenderer>().material = LightBlueMat;
            Module.HandlePass();
        }
        else
        {
            waitTime = 0.4f;
            durationSecond = 0.4f;
            yPos = -0.04f;
            Module.HandleStrike();
        }
        yield return new WaitForSeconds(waitTime);
        while (elapsedSecond < durationSecond)
        {
            StatusLightObj.transform.localPosition = new Vector3(xPos[j], Easing.InOutQuad(elapsedSecond, 0.05f, yPos, durationSecond), zPos[j]);
            yield return null;
            elapsedSecond += Time.deltaTime;
        }
        StatusLightObj.transform.localPosition = new Vector3(xPos[j], yPos, zPos[j]);
        if (!_moduleSolved)
        {
            yield return new WaitForSeconds(0.2f);
            SetHexesBlack();
            _areHexesBlack = true;
            yield return new WaitForSeconds(1.0f);
            _firstFlash = true;
            SetHexMaterials();
            PickStartingHex();
            DecideCorrectHexes();
            _areHexesBlack = false;
        }
        _areHexesFlashing = false;
    }

    private IEnumerator ShowOffStrike()
    {
        _showOff = false;
        ShowOffText.text = "";
        var duration = 0.3f;
        var elapsed = 0f;
        Audio.PlaySoundAtTransform("HexOpen", transform);
        while (elapsed < duration)
        {
            HexesParent.transform.localEulerAngles = new Vector3(Easing.InOutQuad(elapsed, 0f, 90f, duration), 0f, 0f);
            yield return null;
            elapsed += Time.deltaTime;
        }
        HexesParent.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
        Audio.PlaySoundAtTransform("ShowOff", transform);

        ShowOffText.text = "THIS";
        yield return new WaitForSeconds(0.374f);
        ShowOffText.text = "IS";
        yield return new WaitForSeconds(0.149f);
        ShowOffText.text = "WHAT";
        yield return new WaitForSeconds(0.16f);
        ShowOffText.text = "HAPPENS";
        yield return new WaitForSeconds(0.4f);
        ShowOffText.text = "WHEN";
        yield return new WaitForSeconds(0.145f);
        ShowOffText.text = "YOU";
        yield return new WaitForSeconds(0.149f);
        ShowOffText.text = "SHOW";
        yield return new WaitForSeconds(0.3f);
        ShowOffText.text = "OFF";
        yield return new WaitForSeconds(0.32f);
        ShowOffText.text = "";

        StatusLightObj.transform.localScale = new Vector3(4f, 4f, 4f);
        var durationFirst = 0.3f;
        var elapsedFirst = 0f;
        Audio.PlaySoundAtTransform("Hooo", transform);
        while (elapsedFirst < durationFirst)
        {
            StatusLightObj.transform.localPosition = new Vector3(0f, Easing.InOutQuad(elapsedFirst, -0.2f, 0.05f, durationFirst), 0f);
            yield return null;
            elapsedFirst += Time.deltaTime;
        }
        Module.HandleStrike();
        yield return new WaitForSeconds(0.5f);
        var durationSecond = 0.3f;
        var elapsedSecond = 0f;
        while (elapsedSecond < durationSecond)
        {
            StatusLightObj.transform.localPosition = new Vector3(0f, Easing.InOutQuad(elapsedSecond, 0.05f, -0.2f, durationSecond), 0f);
            yield return null;
            elapsedSecond += Time.deltaTime;
        }
        StatusLightObj.transform.localPosition = new Vector3(0f, -0.2f, 0f);
        yield return new WaitForSeconds(0.2f);
        var durationThird = 0.3f;
        var elapsedThird = 0f;
        while (elapsedThird < durationThird)
        {
            HexesParent.transform.localEulerAngles = new Vector3(Easing.InOutQuad(elapsedThird, 90f, 0f, durationThird), 0f, 0f);
            yield return null;
            elapsedThird += Time.deltaTime;
        }
        Audio.PlaySoundAtTransform("DoorClose", transform);
        HexesParent.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
        StatusLightObj.transform.localScale = new Vector3(1f, 1f, 1f);
        SetHexesBlack();
        _areHexesBlack = true;
        yield return new WaitForSeconds(1.0f);
        _firstFlash = true;
        SetHexMaterials();
        PickStartingHex();
        DecideCorrectHexes();
        _areHexesBlack = false;
        _areHexesFlashing = false;
    }

    private void Update()
    {
        if (_areHexesRed && _areHexesFlashing)
            CheckForCorrectHover();
    }

    private void CheckForCorrectHover()
    {
        if (!_isHexHighlighted[_currentHex] && _canStagesContinue)
        {
            _canStagesContinue = false;
            Debug.LogFormat("[Mayhem #{0}] The correct hex was not remained highlighted for entire duration of hexes being red. Strike.", _moduleId);
        }
    }
#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} U DR wait UL D wait (etc.) [activate module, wait, step in those directions, wait, step again etc.]";
#pragma warning restore 0414
    private static readonly Hex[] _hexes = new Hex[]
    {
        new Hex(-2, 0),
        new Hex(-2, 1),
        new Hex(-2, 2),
        new Hex(-1, -1),
        new Hex(-1, 0),
        new Hex(-1, 1),
        new Hex(-1, 2),
        new Hex(0, -2),
        new Hex(0, -1),
        new Hex(0, 0),
        new Hex(0, 1),
        new Hex(0, 2),
        new Hex(1, -2),
        new Hex(1, -1),
        new Hex(1, 0),
        new Hex(1, 1),
        new Hex(2, -2),
        new Hex(2, -1),
        new Hex(2, 0)
    };

    IEnumerator ProcessTwitchCommand(string command)
    {
        var pieces = command.ToLowerInvariant().Trim().Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        var elements = new int?[pieces.Length];
        var numWaits = 0;
        var curHex = _hexes[_startingHex];

        for (var i = 0; i < pieces.Length; i++)
        {
            switch (pieces[i])
            {
                case "ul": case "nw": case "10": curHex = curHex.GetNeighbor(0); break;
                case "u": case "n": case "12": curHex = curHex.GetNeighbor(1); break;
                case "ur": case "ne": case "2": curHex = curHex.GetNeighbor(2); break;
                case "dr": case "se": case "4": curHex = curHex.GetNeighbor(3); break;
                case "d": case "s": case "6": curHex = curHex.GetNeighbor(4); break;
                case "dl": case "sw": case "8": curHex = curHex.GetNeighbor(5); break;
                case "wait": case "w": case ".": elements[i] = null; numWaits++; continue;
            }
            if (curHex.Distance > 2)
            {
                yield return string.Format("sendtochaterror Step #{0} would move you out of bounds.", i + 1);
                yield break;
            }
            elements[i] = Array.IndexOf(_hexes, curHex);
        }

        if (pieces.Length > 30)
            _showOff = true;

        if (numWaits != 5)
        {
            yield return "sendtochaterror I expected exactly 5 “wait”s.";
            yield break;
        }

        yield return null;

        yield return RunTPSequence(elements, isSolver: false);

        HexHighlightEnd(Array.IndexOf(_hexes, curHex));
        yield return "end multiple strikes";
        yield return "solve";
    }

    private IEnumerator RunTPSequence(IEnumerable<int?> elements, bool isSolver)
    {
        HexSelectables[_startingHex].OnInteract();

        HexHighlight(_startingHex);
        while (!_areHexesRed)
            yield return null;
        while (_areHexesRed)
            yield return null;

        var prevHex = _startingHex;

        foreach (var tr in elements)
        {
            if (tr == null)
            {
                if (!isSolver)
                    yield return "multiple strikes";    // This tells TP not to abort the handler upon a strike
                while (!_areHexesRed)
                    yield return null;
                var mustAbort = !_canStagesContinue;
                while (_areHexesRed)
                    yield return null;
                if (mustAbort)
                {
                    HexHighlightEnd(prevHex);
                    yield break;
                }
            }
            else
            {
                HexHighlightEnd(prevHex);
                HexHighlight(tr.Value);
                yield return new WaitForSeconds(isSolver ? .05f : .025f);
                prevHex = tr.Value;
            }
        }

        while (!_areHexesRed)
            yield return null;
        while (_areHexesRed)
            yield return null;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        var elements = new List<int?>();

        var curHex = _hexes[_startingHex];
        for (var i = 1; i < 7; i++)
        {
            var goal = _hexes[_correctHexes[i]];

            while (curHex != goal)
            {
                var diff = goal - curHex;
                int movement;
                if (diff.Q > 0 && diff.R < 0)
                    movement = 2;
                else if (diff.Q > 0)
                    movement = 3;
                else if (diff.Q < 0 && diff.R > 0)
                    movement = 5;
                else if (diff.Q < 0)
                    movement = 0;
                else if (diff.R > 0)
                    movement = 4;
                else
                    movement = 1;
                curHex = curHex.GetNeighbor(movement);
                elements.Add(Array.IndexOf(_hexes, curHex));
            }
            elements.Add(null);
        }
        elements.RemoveAt(elements.Count - 1);

        var e = RunTPSequence(elements, isSolver: true);
        while (e.MoveNext())
            yield return e.Current;

        while (!_moduleSolved)
            yield return true;
    }
}
