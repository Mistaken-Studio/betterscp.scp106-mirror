// -----------------------------------------------------------------------
// <copyright file="Translation.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Exiled.API.Interfaces;

namespace Mistaken.BetterSCP.SCP106
{
    /// <inheritdoc/>
    public class Translation : ITranslation
    {
        public string StartMessage { get; set; } = "<color=red><b><size=500%>UWAGA</size></b></color><br><br><br><br><br><br><size=90%>Rozgrywka jako <color=red>SCP 106</color> na tym serwerze jest zmodyfikowana, <color=red>SCP 106</color> po użyciu przycisku do <color=yellow>tworzenia portalu</color> przeniesie się do <color=yellow>losowego</color> pomieszczenia w <color=yellow><b>innej</b></color> strefie niż ta w której obecnie się znajduje<br><br>Przycisk do <color=yellow>użycia portalu</color> działa tak samo tylko że przenosi do <color=yellow>losowego</color> pomieszczenia w <color=yellow><b>tej samej</b></color> strefie w jakiej znajduje się <color=red>SCP 106</color></size>";
    }
}
