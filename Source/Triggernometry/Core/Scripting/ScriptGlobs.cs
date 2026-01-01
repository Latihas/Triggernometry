namespace Triggernometry.Core.Scripting
{
    public class ScriptGlobs
    {
        public ScriptContextHelper TriggernometryHelpers { get; set; }

        internal ScriptGlobs(Context ctx)
        {
            TriggernometryHelpers = new ScriptContextHelper()
            {
                CurrentContext = ctx
            };
        }
    }
}
