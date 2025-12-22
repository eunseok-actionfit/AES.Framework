using System.Collections.Generic;

public static class LoadingUIRegistry
{
    private sealed class Entry
    {
        public ILoadingOverlayUI Ui;
        public int Priority;
    }

    private readonly static List<Entry> Entries = new();

    public static void Register(ILoadingOverlayUI ui, int priority)
    {
        if (ui == null) return;

        for (int i = 0; i < Entries.Count; i++)
        {
            if (ReferenceEquals(Entries[i].Ui, ui))
            {
                Entries[i].Priority = priority;
                return;
            }
        }

        Entries.Add(new Entry { Ui = ui, Priority = priority });
    }

    public static void Unregister(ILoadingOverlayUI ui)
    {
        if (ui == null) return;
        Entries.RemoveAll(e => ReferenceEquals(e.Ui, ui));
    }

    public static ILoadingOverlayUI Current
    {
        get
        {
            ILoadingOverlayUI best = null;
            int bestP = int.MinValue;

            for (int i = 0; i < Entries.Count; i++)
            {
                var e = Entries[i];
                if (e.Ui == null) continue;
                if (e.Priority >= bestP)
                {
                    bestP = e.Priority;
                    best = e.Ui;
                }
            }

            return best;
        }
    }
}