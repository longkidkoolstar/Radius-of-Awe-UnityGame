using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine.Events;
using System.Reflection;

public class BuildLevel5Editor
{
    private static void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(obj, value);
        }
        else
        {
            Debug.LogWarning($"Field {fieldName} not found on {obj.GetType()}");
        }
    }

    [MenuItem("Tools/Build Level 5")]
    public static void BuildLevel5()
    {
        GameObject root = GameObject.Find("--- LEVEL FIVE: THE QUANTUM THRESHOLD ---");
        if (root == null)
        {
            root = new GameObject("--- LEVEL FIVE: THE QUANTUM THRESHOLD ---");
        }

        // Clear existing children
        int childCount = root.transform.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
        }

        // 1. SECTION 1: THE RIFT
        // Ground Start
        GameObject groundStart = CreatePlatform("Ground_Start", new Vector2(0f, -1f), new Vector2(15f, 2f), root.transform);
        
        // Danger Floor 1
        GameObject dangerFloor1 = CreateDangerFloor("Danger_Floor_1", new Vector2(11.25f, -6f), new Vector2(7.5f, 2f), root.transform);

        // Portal A (Entrance)
        WonderPortal portalA = CreatePortal("WonderPortal_A", new Vector2(11.25f, -4.5f), 0f, root.transform);
        
        // Ground Tutorial Exit
        GameObject groundExit = CreatePlatform("Ground_Tutorial_Exit", new Vector2(20f, 3f), new Vector2(10f, 2f), root.transform);
        
        // Portal B (Exit)
        WonderPortal portalB = CreatePortal("WonderPortal_B", new Vector2(16f, 6f), -90f, root.transform);

        // Link Portal A and Portal B
        portalA.linkedPortal = portalB;
        portalA.exitPoint = portalA.transform.Find("ExitPoint");
        
        portalB.linkedPortal = portalA;
        portalB.exitPoint = portalB.transform.Find("ExitPoint");

        // 2. SECTION 2: CRATE TELEPORTATION
        // Ground Crate Area
        GameObject groundCrate = CreatePlatform("Ground_CrateArea", new Vector2(32f, 2f), new Vector2(14f, 2f), root.transform);

        // Sliding Gate
        GameObject gateObj = new GameObject("ExitGate_1");
        gateObj.transform.SetParent(root.transform);
        gateObj.transform.localPosition = new Vector3(34f, 5f, 0f);
        gateObj.transform.localScale = new Vector3(1f, 4f, 1f);
        var gateSr = gateObj.AddComponent<SpriteRenderer>();
        gateSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/ExitGate.png");
        gateObj.AddComponent<BoxCollider2D>();
        SlidingGate gate = gateObj.AddComponent<SlidingGate>();
        SetPrivateField(gate, "slideOffset", new Vector3(0f, 4.5f, 0f));
        SetPrivateField(gate, "duration", 1.4f);

        // Button Chamber platform
        GameObject buttonCeiling = CreatePlatform("Button_Ceiling", new Vector2(32f, 12f), new Vector2(4f, 1f), root.transform);

        // Ceiling Button
        GameObject buttonObj = new GameObject("CeilingPressurePlate_1");
        buttonObj.transform.SetParent(root.transform);
        buttonObj.transform.localPosition = new Vector3(32f, 11.5f, 0f);
        buttonObj.transform.localScale = Vector3.one;
        buttonObj.AddComponent<BoxCollider2D>();
        CeilingButton button = buttonObj.AddComponent<CeilingButton>();
        SetPrivateField(button, "requireWonderObject", true);
        
        // Connect event using editor-safe event tools
        var onPressedEvent = button.OnPressed;
        if (onPressedEvent == null)
        {
            onPressedEvent = new UnityEvent();
            SetPrivateField(button, "onPressed", onPressedEvent);
        }
        UnityEventTools.AddPersistentListener(onPressedEvent, gate.OpenGate);

        // Bouncer Up
        GameObject bouncerObj = new GameObject("Bouncer_Up");
        bouncerObj.transform.SetParent(root.transform);
        bouncerObj.transform.localPosition = new Vector3(26f, 3.5f, 0f);
        bouncerObj.transform.localScale = new Vector3(2.5f, 1f, 1f);
        bouncerObj.AddComponent<BoxCollider2D>();
        WonderBouncer bouncer = bouncerObj.AddComponent<WonderBouncer>();
        SetPrivateField(bouncer, "bounceDirection", Vector2.up);
        SetPrivateField(bouncer, "bounceForce", 18f);

        // Portal C (Entrance)
        WonderPortal portalC = CreatePortal("WonderPortal_C", new Vector2(26f, 12f), 180f, root.transform);

        // Portal D (Exit)
        WonderPortal portalD = CreatePortal("WonderPortal_D", new Vector2(32f, 7f), 0f, root.transform);

        // Link Portal C and Portal D
        portalC.linkedPortal = portalD;
        portalC.exitPoint = portalC.transform.Find("ExitPoint");

        portalD.linkedPortal = portalC;
        portalD.exitPoint = portalD.transform.Find("ExitPoint");

        // Floaty Crate Key
        GameObject crate = new GameObject("FloatyCrate_Key");
        crate.transform.SetParent(root.transform);
        crate.transform.localPosition = new Vector3(26f, 4.5f, 0f);
        crate.transform.localScale = Vector3.one;
        
        var crateSr = crate.AddComponent<SpriteRenderer>();
        crateSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/PlatformSlate.png");
        
        var crateRb = crate.AddComponent<Rigidbody2D>();
        crateRb.mass = 1f;
        crateRb.drag = 0.8f;
        crateRb.angularDrag = 0.05f;
        crateRb.gravityScale = 3f;
        crateRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        crateRb.interpolation = RigidbodyInterpolation2D.Interpolate;
        crateRb.freezeRotation = true;

        var crateCol = crate.AddComponent<BoxCollider2D>();
        crateCol.size = new Vector2(1f, 1f);

        crate.AddComponent<WonderObject>();
        crate.AddComponent<WonderObjectJuice>();
        crate.AddComponent<RideableFloaty>();

        // 3. SECTION 3: THE QUANTUM GAUNTLET
        // Ground Gauntlet Start
        GameObject groundGauntlet = CreatePlatform("Ground_Gauntlet_Start", new Vector2(38f, 2f), new Vector2(4f, 2f), root.transform);

        // Danger Floor 2 (Spikes)
        GameObject dangerFloor2 = CreateDangerFloor("Danger_Floor_2", new Vector2(52.5f, -6f), new Vector2(25f, 2f), root.transform);

        // High Platform Start
        GameObject highPlatform = CreatePlatform("Platform_High_Start", new Vector2(42f, 15f), new Vector2(4f, 1f), root.transform);

        // Updraft Gauntlet
        GameObject updraftObj = new GameObject("Updraft_Gauntlet");
        updraftObj.transform.SetParent(root.transform);
        updraftObj.transform.localPosition = new Vector3(39f, 7f, 0f);
        updraftObj.transform.localScale = new Vector3(2f, 10f, 1f);
        updraftObj.AddComponent<BoxCollider2D>();
        WonderUpdraft updraft = updraftObj.AddComponent<WonderUpdraft>();
        SetPrivateField(updraft, "upwardForce", 65f);
        SetPrivateField(updraft, "maxLiftVelocity", 6.5f);

        // Shaft Walls
        CreatePlatform("Wall_Shaft_Left", new Vector2(41f, 7f), new Vector2(1f, 10f), root.transform);
        CreatePlatform("Wall_Shaft_Right", new Vector2(45f, 7f), new Vector2(1f, 10f), root.transform);

        // Gravity Well Down (high velocity slingshot settings!)
        GameObject gravityWellObj = new GameObject("GravityWell_Down");
        gravityWellObj.transform.SetParent(root.transform);
        gravityWellObj.transform.localPosition = new Vector3(43f, 7f, 0f);
        gravityWellObj.transform.localScale = new Vector3(3f, 10f, 1f);
        gravityWellObj.AddComponent<BoxCollider2D>();
        GravityWell gravityWell = gravityWellObj.AddComponent<GravityWell>();
        SetPrivateField(gravityWell, "downwardForce", 220f);
        SetPrivateField(gravityWell, "maxDownVelocity", 24f);

        // Portal E (Entrance) at bottom of gravity well
        WonderPortal portalE = CreatePortal("WonderPortal_E", new Vector2(43f, 2.5f), 0f, root.transform);
        // Add exit velocity multiplier of 1.1f for slingshot boost
        SetPrivateField(portalE, "exitVelocityMultiplier", 1.1f);

        // Portal F (Exit)
        WonderPortal portalF = CreatePortal("WonderPortal_F", new Vector2(48f, 12f), -90f, root.transform);

        // Link Portal E and Portal F
        portalE.linkedPortal = portalF;
        portalE.exitPoint = portalE.transform.Find("ExitPoint");

        portalF.linkedPortal = portalE;
        portalF.exitPoint = portalF.transform.Find("ExitPoint");

        // Passable Wall (Bridge blocks that vanish in Wonder Zone)
        GameObject passableWall = new GameObject("PassableWall_Bridge");
        passableWall.transform.SetParent(root.transform);
        passableWall.transform.localPosition = new Vector3(53f, 12f, 0f);
        passableWall.transform.localScale = new Vector3(1f, 4f, 1f);
        var wallSr = passableWall.AddComponent<SpriteRenderer>();
        wallSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/PlatformSlate.png");
        
        var wallCol = passableWall.AddComponent<BoxCollider2D>();
        var wallWo = passableWall.AddComponent<WonderObject>();
        SetPrivateField(wallWo, "mundaneCollider", wallCol);
        SetPrivateField(wallWo, "swapPhysics", false);

        passableWall.AddComponent<WonderObjectJuice>();

        // Bouncer Diagonal (Time Zone launch pad)
        GameObject bouncerDiagObj = new GameObject("Bouncer_Diagonal");
        bouncerDiagObj.transform.SetParent(root.transform);
        bouncerDiagObj.transform.localPosition = new Vector3(57f, 10f, 0f);
        bouncerDiagObj.transform.localScale = new Vector3(2.5f, 1f, 1f);
        bouncerDiagObj.AddComponent<BoxCollider2D>();
        WonderBouncer bouncerDiag = bouncerDiagObj.AddComponent<WonderBouncer>();
        SetPrivateField(bouncerDiag, "bounceDirection", new Vector2(1f, 1f).normalized);
        SetPrivateField(bouncerDiag, "bounceForce", 16f);

        // Ground Victory
        GameObject groundVictory = CreatePlatform("Ground_Victory", new Vector2(63f, 13f), new Vector2(6f, 2f), root.transform);

        // Victory Success Portal
        GameObject victoryPortalObj = new GameObject("VictorySuccessPortal");
        victoryPortalObj.transform.SetParent(root.transform);
        victoryPortalObj.transform.localPosition = new Vector3(63f, 15.5f, 0f);
        victoryPortalObj.transform.localScale = new Vector3(2f, 3f, 1f);
        var vpSr = victoryPortalObj.AddComponent<SpriteRenderer>();
        vpSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/VictoryPortal.png");
        victoryPortalObj.AddComponent<BoxCollider2D>();
        victoryPortalObj.AddComponent<VictoryPortalTrigger>();

        // Reposition Player to (0, 1) to ensure they start on Ground_Start safely
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = new Vector3(0f, 1f, 0f);
            var playerRb = player.GetComponent<Rigidbody2D>();
            if (playerRb != null) playerRb.velocity = Vector2.zero;
        }

        EditorUtility.SetDirty(root);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        
        Debug.Log("<b><color=#00ff88>[LEVEL BUILDER]</color></b>: Level 5 built successfully!");
    }

    private static GameObject CreatePlatform(string name, Vector2 position, Vector2 scale, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.localPosition = position;
        go.transform.localScale = Vector3.one;
        go.layer = 8; // Ground

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/PlatformSlate.png");
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = scale;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = scale;
        return go;
    }

    private static GameObject CreateDangerFloor(string name, Vector2 position, Vector2 scale, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.localPosition = position;
        go.transform.localScale = Vector3.one;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/DangerFloor.png");
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = scale;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = scale;

        return go;
    }

    private static WonderPortal CreatePortal(string name, Vector3 position, float zRotation, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.localPosition = position;
        go.transform.localRotation = Quaternion.Euler(0f, 0f, zRotation);
        go.transform.localScale = new Vector3(2f, 0.4f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/VictoryPortal.png");

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1f, 1f);

        // ExitPoint child
        GameObject exitObj = new GameObject("ExitPoint");
        exitObj.transform.SetParent(go.transform, false);
        exitObj.transform.localPosition = new Vector3(0f, 4.0f, 0f);

        var portal = go.AddComponent<WonderPortal>();
        return portal;
    }
}
