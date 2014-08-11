using System;
using System.Collections.Generic;

namespace Dynamo.UI.Prompts
{
    public class TaskDialogEventArgs : EventArgs
    {
        List<Tuple<int, string, bool>> buttons = null;

        #region Public Operational Methods

        internal TaskDialogEventArgs(Uri imageUri, string dialogTitle,
                                     string summary, string description)
        {
            this.ImageUri = imageUri;
            this.DialogTitle = dialogTitle;
            this.Summary = summary;
            this.Description = description;
        }

        internal void AddLeftAlignedButton(int id, string content)
        {
            if (buttons == null)
                buttons = new List<Tuple<int, string, bool>>();

            buttons.Add(new Tuple<int, string, bool>(id, content, true));
        }

        internal void AddRightAlignedButton(int id, string content)
        {
            if (buttons == null)
                buttons = new List<Tuple<int, string, bool>>();

            buttons.Add(new Tuple<int, string, bool>(id, content, false));
        }

        #endregion

        #region Public Class Properties

        // Settable properties.
        internal int ClickedButtonId { get; set; }
        internal Exception Exception { get; set; }

        // Read-only properties.
        internal Uri ImageUri { get; private set; }
        internal string DialogTitle { get; private set; }
        internal string Summary { get; private set; }
        internal string Description { get; private set; }

        internal IEnumerable<Tuple<int, string, bool>> Buttons
        {
            get { return buttons; }
        }

        #endregion
    }
}