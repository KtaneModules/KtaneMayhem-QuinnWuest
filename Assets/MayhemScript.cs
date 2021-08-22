using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;
using KModkit;
using System.Text.RegularExpressions;

public class MayhemScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMSelectable[] HexSelectables;
    public GameObject[] Hexes, HexFronts, HexBacks;
    public GameObject StatusLightObj;
    public Material[] HexBlueMats;
    public Material LightBlueMat, StartingHexMat, HexRedMat, HexBlackMat;
    public Light[] HexLights;

    private static int _moduleIdCounter = 1;
    private int _moduleId, _startingHex, _currentHex = 99, _highlightedHex = 99;
    private int[] _correctHexes = new int[7];
    private bool _moduleSolved, _areHexesRed, _areHexesFlashing, _canStagesContinue, _areHexesBlack, _firstFlash = true;
    private bool[] _isHexHighlighted = new bool[19];
    private string SerialNumber;
    private string[] sounds = { "Flash1", "Flash2", "Flash3", "Flash4", "Flash5", "Flash6", "Flash7" };
    private float[] xPos = {
        -0.052f, -0.052f, -0.052f,
        -0.026f, -0.026f, -0.026f, -0.026f,
        0f, 0f, 0f, 0f, 0f,
        0.026f, 0.026f, 0.026f, 0.026f,
        0.052f, 0.052f, 0.052f };
    private float[] zPos = {
        0.03f, 0f, -0.03f,
        0.045f, 0.015f, -0.015f, -0.045f,
        0.06f, 0.03f, 0f, -0.03f, -0.06f,
        0.045f, 0.015f, -0.015f, -0.045f,
        0.03f, 0f, -0.03f };

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
        }
        SerialNumber = BombInfo.GetSerialNumber();
        DecideCorrectHexes();
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
        Debug.LogFormat("[Mayhem #{0}] Hexes to highlight are: {1}, {2}, {3}, {4}, {5}, {6}, {7}.", _moduleId,
            _correctHexes[0] + 1,
            _correctHexes[1] + 1,
            _correctHexes[2] + 1,
            _correctHexes[3] + 1,
            _correctHexes[4] + 1,
            _correctHexes[5] + 1,
            _correctHexes[6] + 1
            );
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
                StartCoroutine(OpenHex(_currentHex, false));
                StartCoroutine(MoveLight(_currentHex, false));
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
    private readonly string TwitchHelpMessage = "!{0} 1 2 3 4 5 6 7 | Highlight hexes 1 2 3 4 5 6 7.";
#pragma warning restore 0414
    IEnumerator ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^[0-9]{1,2} [0-9]{1,2} [0-9]{1,2} [0-9]{1,2} [0-9]{1,2} [0-9]{1,2} [0-9]{1,2}$"))
        {
            int[] twitchPlaysInputs = command.Split(' ').Select(piece => int.Parse(piece)).ToArray();
            for (int i = 0; i < twitchPlaysInputs.Length; i++)
                if (twitchPlaysInputs[i] < 1 || twitchPlaysInputs[i] > 19)
                    yield break;

            HexPress(0);
            yield return new WaitForSeconds(1f);
            for (int i = 0; i < twitchPlaysInputs.Length; i++)
            {
                yield return new WaitForSeconds(0.31f);
                HexHighlight(twitchPlaysInputs[i] - 1);
                yield return new WaitForSeconds(3.31f);
                HexHighlightEnd(twitchPlaysInputs[i] - 1);
                yield return new WaitForSeconds(0.2f);
                if (!_canStagesContinue)
                    break;
            }
        }
    }
    IEnumerator TwitchHandleForcedSolve()
    {
        HexPress(0);
        yield return new WaitForSeconds(1f);
        for (int i = 0; i < _correctHexes.Length; i++)
        {
            yield return new WaitForSeconds(0.31f);
            HexHighlight(_correctHexes[i]);
            yield return new WaitForSeconds(3.31f);
            HexHighlightEnd(_correctHexes[i]);
            yield return new WaitForSeconds(0.2f);
        }
    }
}
