using System;
using System.Collections.Generic;
using System.Linq;
using Zoo;
using UnityEngine;
using System.Collections;

using Rnd = UnityEngine.Random;

/// <summary>
/// On the Subject of Zoos
/// Created by Timwi
/// </summary>
public class ZooModule : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMAudio Audio;
    public KMSelectable MainSelectable;

    public KMSelectable Door;
    public GameObject PedestalsParent;
    public GameObject Animal;
    public Mesh PlaneMesh;

    private KMSelectable[] _pedestals;
    private int[] _pedestalChildIndexes;
    private AnimalInfo[] _solutionLine;

    private bool _isDoorOpen;
    private bool _isSolved;
    private static int _moduleIdCounter = 1;
    private int _moduleId;

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        _isDoorOpen = false;
        _isSolved = false;
        StartCoroutine(Initialize());
    }

    private IEnumerator Initialize()
    {
        var portTypes = Enum.GetValues(typeof(PortType)).Cast<PortType>().ToArray();

        var portTypeToDir = new int[6];
        portTypeToDir[(int) PortType.Parallel] = 0;
        portTypeToDir[(int) PortType.DVI] = 1;
        portTypeToDir[(int) PortType.StereoRCA] = 2;
        portTypeToDir[(int) PortType.Serial] = 3;
        portTypeToDir[(int) PortType.PS2] = 4;
        portTypeToDir[(int) PortType.RJ45] = 5;

        // Wait a little to ensure that edgework is available.
        yield return new WaitForSeconds(.1f);

        var counts = Ut.NewArray(Bomb.GetPortPlateCount() + 1, j => j == 0 ? portTypes.ToHashSet() : new HashSet<PortType>());
        foreach (var port in Bomb.GetPorts())
        {
            var pIx = portTypes.IndexOf(pt => pt.ToString() == port);
            if (pIx != -1)
            {
                var pt = portTypes[pIx];
                var ix = counts.IndexOf(c => c.Contains(pt));
                counts[ix].Remove(pt);
                counts[ix + 1].Add(pt);
            }
        }

        var hexes = Hex.LargeHexagon(5).Select(hex =>
        {
            // Check the port types in order of most common to least common.
            for (int c = counts.Length - 1; c >= 0; c--)
            {
                // Check which of these port types can form a line of 5
                var eligiblePortTypes = counts[c].Where(pt => Enumerable.Range(0, 5).All(dist => (hex + dist * Hex.GetDirection(portTypeToDir[(int) pt])).Distance < 5)).Take(2).ToArray();
                if (eligiblePortTypes.Length != 1)
                    continue;

                // We found an eligible port type; return the line
                return new { PortType = (PortType?) eligiblePortTypes[0], Line = Enumerable.Range(0, 5).Select(dist => hex + dist * Hex.GetDirection(portTypeToDir[(int) eligiblePortTypes[0]])).ToArray() };
            }

            // Check if the two-step rule works for this hex
            for (int dir = 0; dir < 6; dir++)
                if (Enumerable.Range(0, 5).All(dist => (hex + 2 * dist * Hex.GetDirection(dir)).Distance < 5))
                    return new { PortType = (PortType?) null, Line = Enumerable.Range(0, 5).Select(dist => hex + 2 * dist * Hex.GetDirection(dir)).ToArray() };

            return null;
        }).Where(h => h != null);

        var inf = hexes.PickRandom();
        Debug.LogFormat("[Zoo #{0}] Animals shown on front: {1}, {2}", _moduleId, Data.Qs[inf.Line[0].Q + 4].Name, Data.Rs[inf.Line[0].R + 4].Name);
        Debug.LogFormat("[Zoo #{0}] Line: {1} ({2})", _moduleId, inf.Line.Select(h => Data.Hexes[h].Name).JoinString(", "), inf.PortType == null ? "two-steps rule" : inf.PortType.ToString());

        _pedestals = MainSelectable.Children.Where(p => p != null && p.transform.parent == PedestalsParent.transform).ToArray();
        _pedestalChildIndexes = new int[_pedestals.Length];
        for (int i = 0; i < _pedestals.Length; i++)
        {
            _pedestals[i].gameObject.SetActive(false);
            _pedestalChildIndexes[i] = Array.IndexOf(MainSelectable.Children, _pedestals[i]);
        }

        // Put the coordinate animals on the front of the door
        var mult = Rnd.Range(0, 2) == 0 ? -1 : 1;
        CreateGraphic(Data.Qs[inf.Line[0].Q + 4], .35f * mult, -.0499f, -.35f * mult, .07f, Door.transform);
        CreateGraphic(Data.Rs[inf.Line[0].R + 4], -.35f * mult, -.0499f, .35f * mult, .07f, Door.transform);

        // Remember the solution line
        _solutionLine = inf.Line.Select(h => Data.Hexes[h]).ToArray();

        Door.OnInteract = delegate
        {
            Door.AddInteractionPunch();
            if (!_isDoorOpen && !_isSolved)
                StartCoroutine(OpenDoor());
            return false;
        };
    }

    private GameObject CreateGraphic(AnimalInfo animal, float x, float y, float z, float scale, Transform parent)
    {
        var graphic = Instantiate(Animal);
        graphic.name = animal.Name;
        graphic.transform.parent = parent;
        graphic.transform.localPosition = new Vector3(x, y, z);
        graphic.transform.localRotation = Quaternion.Euler(0, 180, 0);
        graphic.transform.localScale = new Vector3(scale, scale, scale);
        graphic.AddComponent<MeshFilter>().mesh = PlaneMesh;
        var mr = graphic.AddComponent<MeshRenderer>();
        var tex = new Texture2D(2, 2);
        tex.LoadImage(animal.Png);
        mr.material.mainTexture = tex;
        mr.material.shader = Shader.Find("Unlit/Transparent");
        return graphic;
    }

    private float easeInOutQuad(float time, float start, float end, float duration)
    {
        time /= duration / 2;
        if (time < 1)
            return (end - start) / 2 * time * time + start;
        time--;
        return -(end - start) / 2 * (time * (time - 2) - 1) + start;
    }

    private IEnumerator OpenDoor()
    {
        _isDoorOpen = true;
        yield return null;

        // Select three out of the five correct animals
        var selection = _solutionLine.ToArray().Shuffle().Take(3).ToList();
        // Select three out of the remaining wrong animals
        selection.AddRange(Data.Hexes.Values.Where(ai => !_solutionLine.Contains(ai)).ToArray().Shuffle().Take(3));
        selection.Shuffle();

        var subprogress = _solutionLine.IndexOf(selection.Contains);
        var abort = false;

        // Select six of the pedestals to display these on
        foreach (var pedestal in _pedestals)
            pedestal.gameObject.SetActive(false);
        var pedestalIxs = Enumerable.Range(0, _pedestals.Length).ToArray().Shuffle().Take(6).ToArray();
        var pedestals = pedestalIxs.Select(ix => _pedestals[ix]).ToArray();
        var graphics = new List<GameObject>();
        for (int i = 0; i < 6; i++)
        {
            graphics.Add(CreateGraphic(selection[i], 0, .2501f, 0, .16f, pedestals[i].transform));
            pedestals[i].gameObject.SetActive(true);

            // Need an extra function scope to work around bug in Mono C# compiler
            pedestals[i].OnInteract = new Func<int, KMSelectable.OnInteractHandler>(j => delegate
            {
                pedestals[j].AddInteractionPunch(.5f);
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pedestals[j].transform);
                if (abort)
                    return false;
                if (selection[j] != _solutionLine[subprogress])
                {
                    Debug.LogFormat("[Zoo #{0}] You pressed {1}, I expected {2}. Strike.", _moduleId, selection[j].Name, _solutionLine[subprogress].Name);
                    Module.HandleStrike();
                    abort = true;
                }
                else
                {
                    Debug.LogFormat("[Zoo #{0}] {1} was correct.", _moduleId, selection[j].Name);
                    do
                        subprogress++;
                    while (subprogress < _solutionLine.Length && !selection.Contains(_solutionLine[subprogress]));
                    if (subprogress == _solutionLine.Length)
                    {
                        Debug.LogFormat("[Zoo #{0}] Module solved.", _moduleId);
                        Module.HandlePass();
                        _isSolved = true;
                        abort = true;
                    }
                }
                return false;
            })(i);
        }

        Debug.LogFormat("[Zoo #{0}] Animals shown behind door: {1}", _moduleId, selection.Select(s => s.Name).JoinString(", "));

        // SLIDE OPEN
        Audio.PlaySoundAtTransform("SlidingSound", Door.transform);
        for (float t = 0; t < 1; t += Time.deltaTime * 1.5f)
        {
            Door.transform.localPosition = new Vector3(easeInOutQuad(t, 0, -.14f, 1), .025f, 0);
            yield return null;
        }
        Door.transform.localPosition = new Vector3(-.14f, .025f, 0);

        for (int i = 0; i < 50 && !abort; i++)
            yield return new WaitForSeconds(.1f);

        // SLIDE CLOSED
        Audio.PlaySoundAtTransform("SlidingSound", Door.transform);
        for (float t = 0; t < 1; t += Time.deltaTime * 1.5f)
        {
            Door.transform.localPosition = new Vector3(easeInOutQuad(t, -.14f, 0, 1), .025f, 0);
            yield return null;
        }
        Door.transform.localPosition = new Vector3(0, .025f, 0);

        if (!abort)
        {
            Debug.LogFormat("[Zoo #{0}] Not enough animals pressed before door closed. Strike.", _moduleId);
            Module.HandleStrike();
        }

        foreach (var graphic in graphics)
            Destroy(graphic);
        _isDoorOpen = false;
    }
}
