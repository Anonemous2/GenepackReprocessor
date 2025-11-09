using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace GenepackReprocessor.Utilities
{
    internal static class DebugMessaging
    {
        private static bool PerformDebugActions;

        static DebugMessaging()
        {
            PerformDebugActions = false;
#if DEBUG
            PerformDebugActions = true;
#endif
        }

        #region Debug Strings
        private static string Debug_BuildRectString(Rect rect, string rectName)
        {
            var debugMsg = $"{rectName}: {{ {nameof(rect.yMin)}: {rect.yMin}; {nameof(rect.yMax)}: {rect.yMax}; {nameof(rect.xMin)}: {rect.xMin}; {nameof(rect.xMax)}: {rect.xMax}; {nameof(rect.width)}: {rect.width}; {nameof(rect.height)}: {rect.height} }}; ... ";
            return debugMsg;
        }

        private static string Debug_BuildScrollbarPositionString(Vector2 scrollPosition)
        {
            var debugMsg = $"{nameof(scrollPosition)}: {{ {nameof(scrollPosition.x)}: {scrollPosition.x}; {nameof(scrollPosition.y)}: {scrollPosition.y} }} ... ";
            return debugMsg;
        }

        private static string Debug_BuildScrollBarVisibleString(bool scrollBarVisible)
        {
            var debugMsg = $"{nameof(scrollBarVisible)}:{scrollBarVisible} ... ";
            return debugMsg;
        }
        #endregion

        public static void DebugRect(Rect rect, string rectName)
        {
            if (PerformDebugActions)
            {
                var debugMsg = Debug_BuildRectString(rect, rectName);
                Messages.Message(debugMsg, null, MessageTypeDefOf.TaskCompletion, historical: false);
            }
        }

        public static void DebugRect_Highlight(Rect rect)
        {
            if (PerformDebugActions && Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }
        }

        public static void DebugRect_ScrollBar(Rect rect, string rectName, bool scrollBarVisible, Vector2 scollbarPosition)
        {
            if (PerformDebugActions)
            {
                var debugMsg = Debug_BuildRectString(rect, rectName) + Debug_BuildScrollBarVisibleString(scrollBarVisible) + Debug_BuildScrollbarPositionString(scollbarPosition);
                Messages.Message(debugMsg, null, MessageTypeDefOf.TaskCompletion, historical: false);
            }
        }

        public static void DebugRect_ScrollVisible(Rect rect, string rectName, bool scrollBarVisible)
        {
            if (PerformDebugActions)
            {
                var debugMsg = Debug_BuildRectString(rect, rectName) + Debug_BuildScrollBarVisibleString(scrollBarVisible);
                Messages.Message(debugMsg, null, MessageTypeDefOf.TaskCompletion, historical: false);
            }
        }

        public static void DebugMessage_JobEnabled(string name, bool setting)
        {
            if (PerformDebugActions)
            {
                var debugMsg = $"Job {name} enabled: {setting}";
                Messages.Message(debugMsg, null, MessageTypeDefOf.NeutralEvent, historical: false);
            }
        }

        public static void DebugMessage_WorkJob(WorkJob job)
        {
            if (PerformDebugActions)
            {
                var debugMsg = $"Current job is: {Enum.GetName(typeof(WorkJob), job)}";
                Messages.Message(debugMsg, null, MessageTypeDefOf.NeutralEvent, historical: false);
            }
        }
    }
}
