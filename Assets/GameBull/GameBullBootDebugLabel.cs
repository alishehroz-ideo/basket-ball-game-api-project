// Drop this on a GameObject in the splash scene to see the parsed params on screen.
using UnityEngine;

namespace GameBull
{
    public class GameBullBootDebugLabel : MonoBehaviour
    {
        void OnGUI()
        {
            GUI.skin.label.fontSize = 18;
            string text =
                "GameBull boot params:\n" +
                $"tenantSlug = {GameBullBoot.TenantSlug}\n" +
                $"gameId     = {GameBullBoot.GameId}\n" +
                $"mode       = {GameBullBoot.Mode}\n" +
                $"seed       = {GameBullBoot.Seed}\n" +
                $"token      = {(string.IsNullOrEmpty(GameBullBoot.SessionToken) ? "(none)" : "present")}\n" +
                $"returnUrl  = {GameBullBoot.ReturnUrl}";
            GUI.Label(new Rect(20, 20, 900, 300), text);
        }
    }
}
