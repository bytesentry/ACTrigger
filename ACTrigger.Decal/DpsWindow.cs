using System.Drawing;
using VirindiViewService;
using VirindiViewService.Controls;

namespace ACTrigger.Decal
{
    public sealed class DpsWindow
    {
        private HudView? _view;
        private HudFixedLayout? _layout;
        private HudStaticText? _labelNames;
        private HudStaticText? _labelValues;

        public void Create()
        {
            if (_view != null)
            {
                _view.LoadUserSettings();
                _view.Visible = true;
                return;
            }

            _view = new HudView(
                "ACTrigger DPS",
                200,
                130,
                new ACImage(Color.Black));

            _layout = new HudFixedLayout();
            _view.Controls.HeadControl = _layout;

            _labelNames = new HudStaticText();
            _labelNames.FontHeight = 12;
            _labelNames.Text =
                "DPS:\n" +
                "Damage:\n" +
                "Max Hit:\n" +
                "Crits:\n" +
                "Time:";

            _labelValues = new HudStaticText();
            _labelValues.FontHeight = 12;
            _labelValues.Text =
                "0\n0\n0\n0\n0.0s";

            _layout.AddControl(
                _labelNames,
                new Rectangle(10, 10, 70, 110));

            _layout.AddControl(
                _labelValues,
                new Rectangle(100, 10, 100, 110));

            _view.UserAlphaChangeable = true;
            _view.UserResizeable = true;
            _view.UserMinimizable = true;
            _view.Location = new Point(200, 200);
            _view.Alpha = 255;
            _view.LoadUserSettings();
            _view.Visible = true;
        }

        public void SetStats(
            int dps,
            long damage,
            int maxHit,
            int criticalHits,
            string time)
        {
            if (_labelValues == null)
                return;

            _labelValues.Text =
                $"{dps:N0}\n" +
                $"{damage:N0}\n" +
                $"{maxHit:N0}\n" +
                $"{criticalHits:N0}\n" +
                time;
        }

        public void Destroy()
        {
            _view?.Dispose();

            _view = null;
            _layout = null;
            _labelNames = null;
            _labelValues = null;
        }
    }
}