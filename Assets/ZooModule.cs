using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Zoo;

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
    public Texture[] AnimalImages;

    private KMSelectable[] _pedestals;
    private int[] _pedestalChildIndexes;
    private string[] _solutionLine;
    private string[] _selection;    // what’s actually shown on the module once the door opens
    private int[] _selectionPedestalIxs;    // what’s actually shown on the module once the door opens

    enum State { DoorClosed, DoorOpen, DoorClosing, Solved }
    private State _state;

    private static int _moduleIdCounter = 1;
    private int _moduleId;

    private static Dictionary<Hex, string> _inGrid;
    private static List<string> _outGridQ = new List<string> { "Gazelle", "Caracal", "Cheetah", "Ocelot", "Sheep", "Caterpillar", "Groundhog", "Armadillo", "Orca" };
    private static List<string> _outGridR = new List<string> { "Plesiosaur", "Penguin", "Baboon", "Whale", "Squid", "Coyote", "Ram", "Deer", "Crocodile" };

    static ZooModule()
    {
        _inGrid = new Dictionary<Hex, string>();
        _inGrid[new Hex(0, -4)] = "Cow";
        _inGrid[new Hex(1, -4)] = "Tyrannosaurus Rex";
        _inGrid[new Hex(2, -4)] = "Rabbit";
        _inGrid[new Hex(3, -4)] = "Horse";
        _inGrid[new Hex(4, -4)] = "Flamingo";
        _inGrid[new Hex(-1, -3)] = "Cat";
        _inGrid[new Hex(0, -3)] = "Bat";
        _inGrid[new Hex(1, -3)] = "Ant";
        _inGrid[new Hex(2, -3)] = "Fly";
        _inGrid[new Hex(3, -3)] = "Llama";
        _inGrid[new Hex(4, -3)] = "Hyena";
        _inGrid[new Hex(-2, -2)] = "Pig";
        _inGrid[new Hex(-1, -2)] = "Owl";
        _inGrid[new Hex(0, -2)] = "Rhinoceros";
        _inGrid[new Hex(1, -2)] = "Tortoise";
        _inGrid[new Hex(2, -2)] = "Sea Horse";
        _inGrid[new Hex(3, -2)] = "Camel";
        _inGrid[new Hex(4, -2)] = "Dimetrodon";
        _inGrid[new Hex(-3, -1)] = "Spider";
        _inGrid[new Hex(-2, -1)] = "Goose";
        _inGrid[new Hex(-1, -1)] = "Snail";
        _inGrid[new Hex(0, -1)] = "Monkey";
        _inGrid[new Hex(1, -1)] = "Wolf";
        _inGrid[new Hex(2, -1)] = "Kangaroo";
        _inGrid[new Hex(3, -1)] = "Lobster";
        _inGrid[new Hex(4, -1)] = "Dromedary";
        _inGrid[new Hex(-4, 0)] = "Bear";
        _inGrid[new Hex(-3, 0)] = "Dragonfly";
        _inGrid[new Hex(-2, 0)] = "Butterfly";
        _inGrid[new Hex(-1, 0)] = "Fox";
        _inGrid[new Hex(0, 0)] = "Dolphin";
        _inGrid[new Hex(1, 0)] = "Eagle";
        _inGrid[new Hex(2, 0)] = "Porcupine";
        _inGrid[new Hex(3, 0)] = "Otter";
        _inGrid[new Hex(4, 0)] = "Warthog";
        _inGrid[new Hex(-4, 1)] = "Ferret";
        _inGrid[new Hex(-3, 1)] = "Lion";
        _inGrid[new Hex(-2, 1)] = "Squirrel";
        _inGrid[new Hex(-1, 1)] = "Giraffe";
        _inGrid[new Hex(0, 1)] = "Koala";
        _inGrid[new Hex(1, 1)] = "Crab";
        _inGrid[new Hex(2, 1)] = "Frog";
        _inGrid[new Hex(3, 1)] = "Swallow";
        _inGrid[new Hex(-4, 2)] = "Stegosaurus";
        _inGrid[new Hex(-3, 2)] = "Pterodactyl";
        _inGrid[new Hex(-2, 2)] = "Cobra";
        _inGrid[new Hex(-1, 2)] = "Hippopotamus";
        _inGrid[new Hex(0, 2)] = "Triceratops";
        _inGrid[new Hex(1, 2)] = "Duck";
        _inGrid[new Hex(2, 2)] = "Starfish";
        _inGrid[new Hex(-4, 3)] = "Elephant";
        _inGrid[new Hex(-3, 3)] = "Rooster";
        _inGrid[new Hex(-2, 3)] = "Woodpecker";
        _inGrid[new Hex(-1, 3)] = "Apatosaurus";
        _inGrid[new Hex(0, 3)] = "Beaver";
        _inGrid[new Hex(1, 3)] = "Gorilla";
        _inGrid[new Hex(-4, 4)] = "Mouse";
        _inGrid[new Hex(-3, 4)] = "Seal";
        _inGrid[new Hex(-2, 4)] = "Skunk";
        _inGrid[new Hex(-1, 4)] = "Viper";
        _inGrid[new Hex(0, 4)] = "Salamander";
    }

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        _state = State.DoorClosed;
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

        _pedestals = MainSelectable.Children.Where(p => p != null && p.transform.parent == PedestalsParent.transform).ToArray();

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
        Debug.LogFormat("[Zoo #{0}] Animals shown on front: {1}, {2}", _moduleId, _outGridQ[inf.Line[0].Q + 4], _outGridR[inf.Line[0].R + 4]);
        Debug.LogFormat("[Zoo #{0}] Line: {1} ({2})", _moduleId, inf.Line.Select(h => _inGrid[h]).JoinString(", "), inf.PortType == null ? "two-steps rule" : inf.PortType.ToString());

        _pedestalChildIndexes = new int[_pedestals.Length];
        for (int i = 0; i < _pedestals.Length; i++)
        {
            _pedestals[i].gameObject.SetActive(false);
            _pedestalChildIndexes[i] = Array.IndexOf(MainSelectable.Children, _pedestals[i]);
        }

        // Put the coordinate animals on the front of the door
        var mult = Rnd.Range(0, 2) == 0 ? -1 : 1;
        CreateGraphic(_outGridQ[inf.Line[0].Q + 4], .35f * mult, -.0499f, -.35f * mult, .07f, Door.transform);
        CreateGraphic(_outGridR[inf.Line[0].R + 4], -.35f * mult, -.0499f, .35f * mult, .07f, Door.transform);

        // Remember the solution line
        _solutionLine = inf.Line.Select(h => _inGrid[h]).ToArray();

        Door.OnInteract = delegate
        {
            Door.AddInteractionPunch();
            if (_state == State.DoorClosed)
                StartCoroutine(OpenDoor());
            return false;
        };
    }

    private GameObject CreateGraphic(string animal, float x, float y, float z, float scale, Transform parent)
    {
        var graphic = Instantiate(Animal);
        graphic.name = animal;
        graphic.transform.parent = parent;
        graphic.transform.localPosition = new Vector3(x, y, z);
        graphic.transform.localRotation = Quaternion.Euler(0, 180, 0);
        graphic.transform.localScale = new Vector3(scale, scale, scale);
        graphic.AddComponent<MeshFilter>().mesh = PlaneMesh;
        var mr = graphic.AddComponent<MeshRenderer>();
        mr.material.mainTexture = AnimalImages.First(at => at.name == animal);
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
        yield return null;

        // Select three out of the five correct animals
        var selection = _solutionLine.ToArray().Shuffle().Take(3).ToList();
        // Select three out of the remaining wrong animals
        selection.AddRange(_inGrid.Values.Where(ai => !_solutionLine.Contains(ai)).ToArray().Shuffle().Take(3));
        selection.Shuffle();
        _selection = selection.ToArray();

        var subprogress = _solutionLine.IndexOf(selection.Contains);
        _state = State.DoorOpen;

        // Select six of the pedestals to display these on
        foreach (var pedestal in _pedestals)
            pedestal.gameObject.SetActive(false);

        _selectionPedestalIxs = Enumerable.Range(0, _pedestals.Length).ToArray().Shuffle().Take(6).ToArray();
        var pedestals = _selectionPedestalIxs.Select(ix => _pedestals[ix]).ToArray();
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
                if (_state != State.DoorOpen)
                    return false;
                if (selection[j] != _solutionLine[subprogress])
                {
                    Debug.LogFormat("[Zoo #{0}] You pressed {1}, I expected {2}. Strike.", _moduleId, selection[j], _solutionLine[subprogress]);
                    Module.HandleStrike();
                    _state = State.DoorClosing;
                }
                else
                {
                    Debug.LogFormat("[Zoo #{0}] {1} was correct.", _moduleId, selection[j]);
                    do
                        subprogress++;
                    while (subprogress < _solutionLine.Length && !selection.Contains(_solutionLine[subprogress]));
                    if (subprogress == _solutionLine.Length)
                    {
                        Debug.LogFormat("[Zoo #{0}] Module solved.", _moduleId);
                        Module.HandlePass();
                        _state = State.Solved;
                    }
                }
                return false;
            })(i);
        }

        Debug.LogFormat("[Zoo #{0}] Animals shown behind door: {1}", _moduleId, selection.JoinString(", "));

        // SLIDE OPEN
        Audio.PlaySoundAtTransform("SlidingSound", Door.transform);
        for (float t = 0; t < 1; t += Time.deltaTime * 1.5f)
        {
            Door.transform.localPosition = new Vector3(easeInOutQuad(t, 0, -.135f, 1), .025f, 0);
            yield return null;
        }
        Door.transform.localPosition = new Vector3(-.135f, .025f, 0);

        for (int i = 0; i < 60 && _state == State.DoorOpen; i++)
            yield return new WaitForSeconds(.1f);

        // SLIDE CLOSED
        Audio.PlaySoundAtTransform("SlidingSound", Door.transform);
        for (float t = 0; t < 1; t += Time.deltaTime * 1.5f)
        {
            Door.transform.localPosition = new Vector3(easeInOutQuad(t, -.135f, 0, 1), .025f, 0);
            yield return null;
        }
        Door.transform.localPosition = new Vector3(0, .025f, 0);

        if (_state == State.DoorOpen)
        {
            Debug.LogFormat("[Zoo #{0}] Not enough animals pressed before door closed. Strike.", _moduleId);
            Module.HandleStrike();
        }

        foreach (var graphic in graphics)
            Destroy(graphic);
        foreach (var pedestal in pedestals)
        {
            pedestal.gameObject.SetActive(false);
            pedestal.OnInteract = null;
        }

        if (_state != State.Solved)
            _state = State.DoorClosed;
    }

#pragma warning disable 414
    private string TwitchHelpMessage = @"“!{0} press animal, animal, ...”; for example: “press Koala, Eagle, Kangaroo, Camel, Hyena”. The module will open the door and automatically press the animals that are there. Type “!{0} animals” to get a list of acceptable animal names.";
#pragma warning restore 414

    private static string _animalListMsg1;
    private static string _animalListMsg2;

    private static void getAnimalListMsgs()
    {
        if (_animalListMsg1 == null)
        {
            var animalNames = _inGrid.Values.OrderBy(v => v).ToArray();
            _animalListMsg1 = string.Format("sendtochat {0},", animalNames.Take(animalNames.Length / 2).JoinString(", "));
            _animalListMsg2 = string.Format("sendtochat {0}.", animalNames.Skip(animalNames.Length / 2).JoinString(", ", lastSeparator: " and "));
        }
    }

    private IEnumerator ProcessTwitchCommand(string command)
    {
        if (command == "animals")
        {
            yield return "sendtochat Acceptable animal names are:";
            getAnimalListMsgs();
            yield return _animalListMsg1;
            yield return _animalListMsg2;
        }

        // The door could still be open from a previous command where someone didn’t press enough animals.
        if (_state != State.DoorClosed)
            yield break;

        var m = Regex.Match(command, @"^press (.*)$");
        if (!m.Success)
            yield break;

        var animals = m.Groups[1].Value.Split(',').Select(str => str.Trim()).ToArray();
        var animalInfos = new KeyValuePair<Hex, string>[animals.Length];
        for (int i = 0; i < animals.Length; i++)
        {
            animalInfos[i] = _inGrid.FirstOrDefault(kvp => kvp.Value.Equals(animals[i], StringComparison.InvariantCultureIgnoreCase));
            if (animalInfos[i].Value == null)
            {
                yield return string.Format("sendtochat What the hell is a {0}?! I only know about the following animals:", animals[i]);
                getAnimalListMsgs();
                yield return _animalListMsg1;
                yield return _animalListMsg2;
                yield break;
            }
        }

        Debug.LogFormat("[Zoo #{0}] Received Twitch Plays command to press: {1}.", _moduleId, animalInfos.Select(i => i.Value).JoinString(", "));
        yield return null;

        Door.OnInteract();
        yield return new WaitForSeconds(1f);

        for (int i = 0; i < animalInfos.Length; i++)
        {
            yield return new WaitForSeconds(.1f);
            var j = Array.IndexOf(_selection, animalInfos[i].Value);
            if (j == -1)
                continue;

            _pedestals[_selectionPedestalIxs[j]].OnInteract();
            if (_state != State.DoorOpen)  // strike or solve
                yield break;
        }

        // If you get here, you didn’t press enough animals.
        // The time will run out and the door will close after some time.
        yield return "strike";
    }
}
