using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Bridge : MonoBehaviour
{
    // Persists across scene loads so it can re-apply the image whenever gameplay loads.
    static Texture2D _pendingTexture;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        var go = new GameObject("Bridge");
        go.AddComponent<Bridge>();
        DontDestroyOnLoad(go);
    }

    void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    // Re-apply whenever any scene (re)loads — catches the gameplay scene loading after menu.
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_pendingTexture != null)
            ApplyTextureToBall(_pendingTexture);
    }

    // Called from JS via unityInstance.SendMessage('Bridge', 'SetBallImage', dataUrl)
    public void SetBallImage(string dataUrl)
    {
        StartCoroutine(DecodeAndApply(dataUrl));
    }

    IEnumerator DecodeAndApply(string dataUrl)
    {
        // Accept either a raw base64 string or a full data URL (data:image/png;base64,...)
        string base64 = dataUrl;
        int comma = dataUrl.IndexOf(',');
        if (comma >= 0)
            base64 = dataUrl.Substring(comma + 1);

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(base64);
        }
        catch (Exception e)
        {
            Debug.LogError("[Bridge] Invalid base64 data: " + e.Message);
            yield break;
        }

        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        if (!tex.LoadImage(bytes))
        {
            Debug.LogError("[Bridge] Texture2D.LoadImage failed — image may be corrupt or unsupported format.");
            yield break;
        }

        _pendingTexture = tex;
        ApplyTextureToBall(tex);
        yield return null;
    }

    static void ApplyTextureToBall(Texture2D tex)
    {
        var ball = GameObject.Find("Ball");
        if (ball == null) return;

        var sr = ball.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        // Scale PPU so the new sprite renders at exactly the same Unity-unit size as the original.
        // original size in units = sprite.rect.width / sprite.pixelsPerUnit
        // new PPU = new tex width / that same size in units
        float ppu = 100f;
        if (sr.sprite != null)
            ppu = tex.width * sr.sprite.pixelsPerUnit / sr.sprite.rect.width;

        sr.sprite = Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            ppu
        );
    }
}
