using UnityEngine;

namespace GameBull
{
    public class GameBullApiTester : MonoBehaviour
    {
        private string _status = "Idle. Tap 'Get Context' to call the API.";

        void OnGUI()
        {
            GUI.skin.button.fontSize = 18;
            GUI.skin.label.fontSize  = 16;

            if (GUI.Button(new Rect(20, 320, 240, 60), "Get Context"))
                _ = RunGetContext();

            GUI.Label(new Rect(20, 390, 1000, 400), _status);
        }

        private async System.Threading.Tasks.Task RunGetContext()
        {
            _status = "Calling GetContext()…";
            var ctx = await GameBullApi.GetContext();
            if (ctx == null) { _status = "GetContext returned null (see Console for the error)."; return; }
            _status =
                "Context received:\n" +
                $"user       = {ctx.user?.displayName}\n" +
                $"points     = {ctx.points}\n" +
                $"lives      = {ctx.lives?.count}/{ctx.lives?.max}\n" +
                $"primary    = {ctx.customization?.colors?.primary}\n" +
                $"secondary  = {ctx.customization?.colors?.secondary}\n" +
                $"logoUrl    = {ctx.customization?.logoUrl}\n" +
                $"modes      = {(ctx.enabledModes == null ? 0 : ctx.enabledModes.Length)}\n" +
                $"assets     = {(ctx.customization?.assets == null ? 0 : ctx.customization.assets.Length)} item(s)";
        }
    }
}
