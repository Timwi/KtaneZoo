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
    public KMRuleSeedable RuleSeedable;

    public KMSelectable Door;
    public GameObject PedestalsParent;
    public GameObject Animal;
    public Texture[] AnimalImages;
    public Material CheatBackground;

    private KMSelectable[] _pedestals;
    private int[] _pedestalChildIndexes;
    private string[] _solutionLine;
    private string[] _selection;    // what’s actually shown on the module once the door opens
    private int[] _selectionPedestalIxs;    // what’s actually shown on the module once the door opens

    enum State { DoorClosed, DoorOpen, DoorClosing, Solved }
    private State _state;

    private static int _moduleIdCounter = 1;
    private int _moduleId;

    private ZooSettings settings = new ZooSettings();

    private static readonly string[] _inGridAnimals = { "Dragonfly", "Kangaroo", "Spider", "Tortoise", "Crab", "Hyena", "Camel", "Butterfly", "Llama", "Dromedary", "Warthog", "Wolf", "Viper", "Lion", "Seal", "Fly", "Lobster", "Cat", "Rooster", "Squirrel", "Gorilla", "Frog", "Salamander", "Goose", "Tyrannosaurus Rex", "Dimetrodon", "Bat", "Starfish", "Swallow", "Pterodactyl", "Porcupine", "Duck", "Apatosaurus", "Koala", "Ferret", "Eagle", "Triceratops", "Stegosaurus", "Bear", "Otter", "Fox", "Beaver", "Owl", "Elephant", "Woodpecker", "Cobra", "Giraffe", "Rhinoceros", "Hippopotamus", "Pig", "Sea Horse", "Mouse", "Flamingo", "Rabbit", "Monkey", "Snail", "Skunk", "Horse", "Dolphin", "Cow", "Ant" };
    private static readonly string[] _outGridQAnimals = { "Groundhog", "Sheep", "Gazelle", "Ocelot", "Cheetah", "Armadillo", "Orca", "Caterpillar", "Caracal" };
    private static readonly string[] _outGridRAnimals = { "Baboon", "Squid", "Whale", "Crocodile", "Deer", "Ram", "Plesiosaur", "Coyote", "Penguin" };
    private static readonly PortType[] _portTypes = { PortType.StereoRCA, PortType.Parallel, PortType.Serial, PortType.PS2, PortType.RJ45, PortType.DVI };

    // Filled in by rule seed
    private Dictionary<Hex, string> _inGrid;
    private string[] _outGridQ;
    private string[] _outGridR;

    void Start()
    {
        var modConfig = new ModConfig<ZooSettings>("ZooSettings");
        settings = modConfig.ReadSettings();
        if (settings.Seconds < 1)
        {
            settings.Seconds = 6;
            Debug.LogFormat("[Zoo #{0}] Seconds count was set to an invalid value, defaulting to 6.", _moduleId);
        }
        else if (settings.Seconds != 6)
        {
            Debug.LogFormat("[Zoo #{0}] Seconds count set to {1}. Official rules are 6 seconds.", _moduleId, settings.Seconds);
            if (settings.Seconds > 6)
                Module.transform.Find("ComponentBackground").GetComponent<MeshRenderer>().sharedMaterial = CheatBackground;
        }
        modConfig.WriteSettings(settings);
        _moduleId = _moduleIdCounter++;


        // ## START RULE SEED

        var rnd = RuleSeedable.GetRNG();
        Debug.Log($"[Zoo #{_moduleId}] Using rule seed: {rnd.Seed}");

        var animals = rnd.ShuffleFisherYates(_inGridAnimals);
        var largeHex = Hex.LargeHexagon(5).ToArray();
        _inGrid = Enumerable.Range(0, largeHex.Length).ToDictionary(ix => largeHex[ix], ix => animals[ix]);
        _outGridQ = rnd.ShuffleFisherYates(_outGridQAnimals.ToArray());
        _outGridR = rnd.ShuffleFisherYates(_outGridRAnimals.ToArray());
        var portTypeFromDir = rnd.ShuffleFisherYates(_portTypes.ToArray());
        var mostCommonFirst = rnd.Next(0, 2) == 0;
        Debug.Log(largeHex.Select(h => $"{h.Q},{h.R}").JoinString(";"));

        // ## END RULE SEED


        _state = State.DoorClosed;

        _pedestals = MainSelectable.Children.Where(p => p != null && p.transform.parent == PedestalsParent.transform).ToArray();

        var counts = Ut.NewArray(Bomb.GetPortPlateCount() + 1, j => j == 0 ? _portTypes.ToHashSet() : new HashSet<PortType>());
        foreach (var port in Bomb.GetPorts())
        {
            var pIx = _portTypes.IndexOf(pt => pt.ToString() == port);
            if (pIx != -1)
            {
                var pt = _portTypes[pIx];
                var ix = counts.IndexOf(c => c.Contains(pt));
                counts[ix].Remove(pt);
                counts[ix + 1].Add(pt);
            }
        }

        var hexes = Hex.LargeHexagon(5).Select(hex =>
        {
            // Check the port types in order of most/least common to least/more common
            for (var c = mostCommonFirst ? counts.Length - 1 : 0; mostCommonFirst ? c >= 0 : c < counts.Length; c = mostCommonFirst ? c - 1 : c + 1)
            {
                // Check which of these port types can form a line of 5
                var eligiblePortTypes = counts[c].Where(pt => Enumerable.Range(0, 5).All(dist => (hex + dist * Hex.GetDirection(Array.IndexOf(portTypeFromDir, pt))).Distance < 5)).Take(2).ToArray();
                if (eligiblePortTypes.Length != 1)
                    continue;

                // We found an eligible port type; return the line
                return new { PortType = (PortType?) eligiblePortTypes[0], Line = Enumerable.Range(0, 5).Select(dist => hex + dist * Hex.GetDirection(Array.IndexOf(portTypeFromDir, eligiblePortTypes[0]))).ToArray() };
            }

            // Check if the two-step rule works for this hex
            for (int dir = 0; dir < 6; dir++)
                if (Enumerable.Range(0, 5).All(dist => (hex + 2 * dist * Hex.GetDirection(dir)).Distance < 5))
                    return new { PortType = (PortType?) null, Line = Enumerable.Range(0, 5).Select(dist => hex + 2 * dist * Hex.GetDirection(dir)).ToArray() };

            return null;
        }).Where(h => h != null);

        var inf = hexes.PickRandom();
        Debug.LogFormat("[Zoo #{0}] Animals shown on front: {1}, {2}", _moduleId, _outGridQ[inf.Line[0].Q + 4], _outGridR[inf.Line[0].R + 4]);
        Debug.LogFormat("[Zoo #{0}] Line: {1} ({2})", _moduleId, inf.Line.Select(h => _inGrid[h]).JoinString(", "), inf.PortType == null ? "two-steps rule" : inf.PortType.Value.ToString());

        _pedestalChildIndexes = new int[_pedestals.Length];
        for (int i = 0; i < _pedestals.Length; i++)
        {
            _pedestals[i].gameObject.SetActive(false);
            _pedestalChildIndexes[i] = Array.IndexOf(MainSelectable.Children, _pedestals[i]);
        }

        // Put the coordinate animals on the front of the door
        var mult = Rnd.Range(0, 2) == 0 ? -1 : 1;
        CreateGraphic(_outGridQ[inf.Line[0].Q + 4], .35f * mult, -.0499f, -.35f * mult, .7f, Door.transform);
        CreateGraphic(_outGridR[inf.Line[0].R + 4], -.35f * mult, -.0499f, .35f * mult, .7f, Door.transform);

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
        graphic.transform.localRotation = Quaternion.Euler(90, 0, 0);
        graphic.transform.localScale = new Vector3(scale, scale, scale);
        graphic.GetComponent<MeshRenderer>().material.mainTexture = AnimalImages.First(at => at.name == animal);
        graphic.SetActive(true);
        return graphic;
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
            graphics.Add(CreateGraphic(selection[i], 0, .2501f, 0, 1.6f, pedestals[i].transform));
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
            Door.transform.localPosition = new Vector3(Easing.InOutQuad(t, 0, -.135f, 1), .025f, 0);
            yield return null;
        }
        Door.transform.localPosition = new Vector3(-.135f, .025f, 0);

        var startTime = Time.time;
        while (Time.time - startTime < settings.Seconds && _state == State.DoorOpen)
            yield return null;
        if (_state != State.DoorOpen)
            Debug.LogFormat("[Zoo #{0}] Door closes with {1} seconds left on the timer.", _moduleId, settings.Seconds - (Time.time - startTime));

        // SLIDE CLOSED
        Audio.PlaySoundAtTransform("SlidingSound", Door.transform);
        for (float t = 0; t < 1; t += Time.deltaTime * 1.5f)
        {
            Door.transform.localPosition = new Vector3(Easing.InOutQuad(t, -.135f, 0, 1), .025f, 0);
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

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = @"“!{0} press animal, animal, ...”; for example: “press Koala, Eagle, Kangaroo, Camel, Hyena”. The module will open the door and automatically press the animals that are there. Type “!{0} animals” to get a list of acceptable animal names.";
#pragma warning restore 0414

    private static string _animalListMsg;

    private static void getAnimalListMsgs()
    {
        if (_animalListMsg == null)
        {
            var animalNames = _inGridAnimals.OrderBy(v => v).ToArray();
            _animalListMsg = string.Format("\n{0},\n{1}.", animalNames.Take(animalNames.Length / 2).JoinString(", "), animalNames.Skip(animalNames.Length / 2).JoinString(", ", lastSeparator: " and "));
        }
    }

    private IEnumerator ProcessTwitchCommand(string command)
    {
        if (command == "animals")
        {
            getAnimalListMsgs();
            yield return "sendtochat Acceptable animal names are:" + _animalListMsg;
        }

        // The door could still be open from a previous command where someone didn’t press enough animals.
        if (_state != State.DoorClosed)
        {
            yield return null;
            yield return "sendtochat I’m going to ignore that command because the door is still open from a previous command.";
            yield break;
        }

        var m = Regex.Match(command, @"^press (.*)$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        if (!m.Success)
            yield break;

        yield return null;

        var animals = m.Groups[1].Value.Split(',').Select(str => str.Trim().Replace(" ", "")).ToArray();
        var animalInfos = new KeyValuePair<Hex, string>[animals.Length];
        for (int i = 0; i < animals.Length; i++)
        {
            animalInfos[i] = _inGrid.FirstOrDefault(kvp => kvp.Value.Trim().Replace(" ", "").Equals(animals[i], StringComparison.InvariantCultureIgnoreCase));
            if (animalInfos[i].Value == null)
            {
                getAnimalListMsgs();
                yield return string.Format("sendtochat What the hell is a “{0}”?! I only know about the following animals:{1}", animals[i], _animalListMsg);
                yield break;
            }
        }

        Debug.LogFormat("[Zoo #{0}] Received Twitch Plays command to press: {1}.", _moduleId, animalInfos.Select(i => i.Value).JoinString(", "));

        Door.OnInteract();
        yield return new WaitForSeconds(1f);

        for (int i = 0; i < animalInfos.Length; i++)
        {
            yield return new WaitForSeconds(.1f);
            var ix = Array.IndexOf(_selection, animalInfos[i].Value);
            if (ix == -1)
                continue;

            _pedestals[_selectionPedestalIxs[ix]].OnInteract();
            if (_state != State.DoorOpen)  // strike or solve
                yield break;
        }

        // If you get here, you didn’t press enough animals.
        // The time will run out and the door will close after some time.
        yield return "strike";
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (_state != State.DoorClosed)
            yield return true;

        Door.OnInteract();
        yield return new WaitForSeconds(1f);

        foreach (var animal in _solutionLine)
        {
            var ix = Array.IndexOf(_selection, animal);
            if (ix == -1)
                continue;
            _pedestals[_selectionPedestalIxs[ix]].OnInteract();
            yield return new WaitForSeconds(.1f);
        }

        while (_state != State.Solved)
            yield return true;
    }

    sealed class ZooSettings
    {
        public int Seconds = 6;
    }

    private static readonly Dictionary<string, object>[] TweaksEditorSettings = Ut.NewArray(new Dictionary<string, object>
    {
        ["Filename"] = "ZooSettings.json",
        ["Name"] = "Zoo",
        ["Listing"] = Ut.NewArray(new Dictionary<string, object>
        {
            ["Key"] = "Seconds count",
            ["Text"] = "The number of seconds for which the door stays open."
        }).ToList()
    });
}
