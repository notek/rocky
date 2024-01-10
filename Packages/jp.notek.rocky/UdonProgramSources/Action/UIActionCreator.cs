
using JP.Notek.Udux;
using VRC.SDK3.Data;

namespace JP.Notek.Rocky.UIAction
{
    public static class ActionCreator
    {
        public static void SendOnOwnershipRequest(this Dispatcher dispatcher, float seek)
        {
            var d = new DataDictionary();
            d["seek"] = (float)seek;
            dispatcher.Dispatch("UI.OnOwnershipRequest", d);
        }
        public static void SendOnPowerOnButtonClicked(this Dispatcher dispatcher)
        {
            dispatcher.Dispatch("UI.OnPowerOnButtonClicked");
        }
        public static void SendOnPowerOffButtonClicked(this Dispatcher dispatcher)
        {
            dispatcher.Dispatch("UI.OnPowerOffButtonClicked");
        }
        public static void SendOnVolumeSliderChanged(this Dispatcher dispatcher, float volume)
        {
            var d = new DataDictionary();
            d["volume"] = volume;
            dispatcher.Dispatch("UI.OnVolumeSliderChanged", d);
        }
        public static void SendOnSubWooferLevelSliderChanged(this Dispatcher dispatcher, float subWooferLevel)
        {
            var d = new DataDictionary();
            d["subWooferLevel"] = (float)subWooferLevel;
            dispatcher.Dispatch("UI.OnSubWooferLevelSliderChanged", d);
        }
        public static void SendOnVideoSeekSliderChanged(this Dispatcher dispatcher, float seek)
        {
            var d = new DataDictionary();
            d["seek"] = (float)seek;
            dispatcher.Dispatch("UI.OnVideoSeekSliderChanged", d);
        }
        public static void SendOnPlayButtonClicked(this Dispatcher dispatcher)
        {
            dispatcher.Dispatch("UI.OnPlayButtonClicked");
        }
    }
}