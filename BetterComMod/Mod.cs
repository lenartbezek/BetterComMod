using System;
using spaar.ModLoader;
using UnityEngine;
using System.Reflection;
using System.Collections;

namespace ComInfo
{

    public class COMInfoMod : Mod
    {
        public override string Name { get; } = "BetterCOMMod";
        public override string DisplayName { get; } = "Better COM Mod";
        public override string Author { get; } = "Lench";
        public override Version Version { get; } = Assembly.GetExecutingAssembly().GetName().Version;

        public override string VersionExtra { get; } = "";
        public override string BesiegeVersion { get; } = "v0.32";

        public override bool CanBeUnloaded { get; } = false;

        public override void OnLoad()
        {
            GameObject.DontDestroyOnLoad(Controller.Instance);
        }

        public override void OnUnload()
        {
            GameObject.Destroy(Controller.Instance);
        }
    }

    public class Controller : SingleInstance<Controller>
    {
        public override string Name { get { return "COM UI Controller"; } }

        public static GameObject panel;
        public static GameObject[] lines;

        public static TextMesh totalMassLabel;
        public static TextMesh totalMassValue;
        public static TextMesh comLabel;
        public static TextMesh comValue;

        private static GameObject comsphere;
        private static GameObject anchor;
        private static GameObject dropdown;
        private static Material textMaterial;
        private static Font textFont;
        private static FontStyle textStyle;

        private static bool showGizmo = false;
        private static bool showPanel = false;
        private static bool updateValues = false;

        private static readonly Vector3 displayedPos = new Vector3(-0.2f, -4.87f, 0);
        private static readonly Vector3 foldedPos = new Vector3(2.2f, -4.87f, 0);

        private static void AddMassChangedDelegate(Transform block)
        {
            var bb = block.GetComponent<BlockBehaviour>();
            if (bb.GetBlockID() == (int)BlockType.Ballast)
            {
#if DEBUG
                Debug.Log("Adding delegate to ballast: " + bb.Guid);
#endif
                bb.Sliders.Find(x => x.Key == "mass").ValueChanged += (float value) => { updateValues = true; };
            }
        }

        private void Awake()
        {
            Game.OnBlockPlaced += (Transform block) => { updateValues = true; };
            Game.OnBlockPlaced += AddMassChangedDelegate;
            Game.OnBlockRemoved += () => { updateValues = true; };
        }

        private void Update()
        {
            // Find COM sphere.
            if (!comsphere)
            {
                comsphere = GameObject.Find("HUD/3D/CenterOfMassVis");
                if (comsphere) InitializeGizmo();
            }

            // Find HUD elements.
            if (!anchor)
            {
                anchor = GameObject.Find("HUD/AlignTopRightNoFold");
                if (anchor) InitializeUI();
            }
            if (!dropdown)
            {
                dropdown = GameObject.Find("HUD/TopBar/AlignTopRight/STATS/SettingsObjects");
            }

            // Show or hide gizmo.
            if (Game.AddPiece != null)
            {
                if (showGizmo != (AddPiece.Instance.comCode.showCOM && !StatMaster.isSimulating))
                {
                    updateValues = true;
                    showGizmo = AddPiece.Instance.comCode.showCOM && !StatMaster.isSimulating;
#if DEBUG
                    if (showGizmo)
                        Debug.Log("Displaying gizmo.");
                    else
                        Debug.Log("Hiding gizmo.");
#endif
                    foreach (var line in lines)
                        line.SetActive(showGizmo);
                }
            }

            // Show or hide panel.
            if (dropdown)
            {
                if (showPanel != (dropdown.transform.localPosition.y < 2f))
                {
                    showPanel = (dropdown.transform.localPosition.y < 2f);
                    if (showPanel)
                        StartCoroutine(ShowPanel());
                    else
                        StartCoroutine(HidePanel());
                }
            }

            // Update values
            if (Game.AddPiece != null && !StatMaster.isSimulating && updateValues)
            {
                updateValues = false;
#if DEBUG
                Debug.Log("Updating values.");
#endif

                // calculate new COM
                var tmp = AddPiece.Instance.comCode.showCOM;
                AddPiece.Instance.comCode.showCOM = true;
                AddPiece.Instance.comCode.GetCOM(Machine.Active().BuildingMachine);
                AddPiece.Instance.comCode.showCOM = tmp;

                totalMassValue.text = Machine.Active().Mass.ToString("0.00");
                var pos = AddPiece.Instance.comCode.CoM - Machine.Active().Position;
                comValue.text = pos.x.ToString("0.00") + ", "+ pos.y.ToString("0.00") + ", " + pos.z.ToString("0.00");
            }
        }

        private static void InitializeGizmo()
        {
#if DEBUG
            Debug.Log("Initializing gizmo.");
#endif

            // CenterOfMassVis transparent shader
            var sphereRenderer = comsphere.GetComponent<Renderer>();
            sphereRenderer.material.shader = Shader.Find("Transparent/Diffuse");
            sphereRenderer.receiveShadows = false;
            sphereRenderer.material.color = new Color(1f, 1f, 1f, 0.5f);

            // Create new gizmo
            lines = new GameObject[3];

            // Prefab
            var line = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            line.layer = comsphere.layer;
            var lineRenderer = line.GetComponent<Renderer>();
            lineRenderer.receiveShadows = false;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.material.color = Color.yellow;
            line.transform.SetParent(comsphere.transform);
            line.transform.localPosition = Vector3.zero;
            line.transform.localScale = new Vector3(0.04f, 10f, 0.04f);
            line.SetActive(showGizmo);

            // Instantiate lines
            lines[0] = (GameObject)Instantiate(line, comsphere.transform);
            lines[0].transform.up = Vector3.forward;
            lines[1] = (GameObject)Instantiate(line, comsphere.transform);
            lines[1].transform.up = Vector3.up;
            lines[2] = (GameObject)Instantiate(line, comsphere.transform);
            lines[2].transform.up = Vector3.right;

            Destroy(line);
        }

        private static void InitializeUI()
        {
#if DEBUG
            Debug.Log("Initializing UI.");
#endif
            var text = GameObject.Find("HUD/AlignTopLeftNoFold/TimeScale/Text");
            textMaterial = new Material(text.GetComponent<Renderer>().material);
            textFont = text.GetComponent<TextMesh>().font;
            textStyle = text.GetComponent<TextMesh>().fontStyle;

            panel = new GameObject("InfoPanel");
            panel.transform.SetParent(anchor.transform, false);
            panel.transform.localPosition = foldedPos;

            var panelbg = (GameObject)Instantiate(GameObject.Find("HUD/AlignTopLeftNoFold/TimeScale/MenuBG"), panel.transform, false);
            panelbg.name = "InfoBG";
            panelbg.transform.localPosition = new Vector3(
                -0.95f,
                3.42f,
                panelbg.transform.localPosition.z);
            panelbg.transform.localScale = new Vector3(
                2.47f,
                1.4f,
                panelbg.transform.localPosition.z);

            GameObject myText;

            myText = (GameObject)Instantiate(text, panel.transform, false);
            myText.name = "MassTitle";
            myText.transform.localPosition = new Vector3(-1.0f, 3.8f, 1f);
            myText.GetComponent<TextMesh>().text = "Machine mass";
            myText.GetComponent<TextMesh>().fontSize = 24;

            // Total mass
            myText = (GameObject)Instantiate(text, panel.transform, false);
            myText.name = "TotalMassLabel";
            myText.transform.localPosition = new Vector3(-1.5f, 3.4f, 1f);
            totalMassLabel = myText.GetComponent<TextMesh>();
            totalMassLabel.text = "Total:";
            totalMassLabel.fontSize = 20;

            myText = (GameObject)Instantiate(text, panel.transform, false);
            myText.name = "TotalMassValue";
            myText.transform.localPosition = new Vector3(-0.5f, 3.4f, 1f);
            totalMassValue = myText.GetComponent<TextMesh>();
            totalMassValue.text = "0.25";
            totalMassValue.fontSize = 20;
            totalMassValue.color = Color.white;

            // COM offset
            myText = (GameObject)Instantiate(text, panel.transform, false);
            myText.name = "ComLabel";
            myText.transform.localPosition = new Vector3(-1.5f, 3.1f, 1f);
            comLabel = myText.GetComponent<TextMesh>();
            comLabel.text = "Offset:";
            comLabel.fontSize = 20;

            myText = (GameObject)Instantiate(text, panel.transform, false);
            myText.name = "ComValue";
            myText.transform.localPosition = new Vector3(-0.5f, 3.1f, 1f);
            comValue = myText.GetComponent<TextMesh>();
            comValue.text = "0.00, 0.00, 0.00";
            comValue.fontSize = 18;
            comValue.color = Color.white;
        }

        private static IEnumerator ShowPanel()
        {
#if DEBUG
            Debug.Log("Showing panel.");
#endif
            var pos = panel.transform.localPosition;
            var t = 0f;
            while (t <= 1)
            {
                if (!showPanel) yield break;
                t += Time.deltaTime;
                pos = Vector3.Lerp(pos, displayedPos, t);
                yield return panel.transform.localPosition = pos;
            }
        }

        private static IEnumerator HidePanel()
        {
#if DEBUG
            Debug.Log("Hiding panel.");
#endif
            var pos = panel.transform.localPosition;
            var t = 0f;
            while (t <= 1)
            {
                if (showPanel) yield break;
                t += Time.deltaTime;
                pos = Vector3.Lerp(pos, foldedPos, t);
                yield return panel.transform.localPosition = pos;
            }
        }
    }
}
