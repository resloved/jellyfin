#nullable disable

using System;
using Jellyfin.Extensions;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Parses JELLYFIN_OWN_WATCHLIST_NAME_TEMPLATE once (e.g. "{username}'s Watchlist"). Drives two
    /// behaviors that must stay in lockstep: pinning each user's own auto-generated watchlist BoxSet
    /// onto their home screen (UserViewManager.GetUserViews), and hiding all such watchlist BoxSets
    /// from the general top-level Collections browse (Folder.QueryRecursive /
    /// BaseItemRepository.TranslateQuery). Unset, missing the "{username}" placeholder, or having no
    /// fixed literal text at all around the placeholder (which would make the hide-filter match every
    /// BoxSet) => both behaviors are a no-op, matching JELLYFIN_PINNED_VIEWS' "absent = no change" rule.
    /// </summary>
    public static class OwnWatchlistNameTemplate
    {
        private const string Placeholder = "{username}";

        // All-letters so GetCleanValue only lowercases it, never replaces/splits it - lets us locate
        // its surviving position in the cleaned full string and recover the true boundary characters
        // (e.g. a space left behind by an apostrophe) instead of losing them to independent trimming.
        private const string Sentinel = "zzzzownwatchlistsentinelzzzz";

        private static readonly ParsedTemplate _parsed = Parse();

        public static bool IsConfigured => _parsed.IsValid;

        public static string CleanPrefix => _parsed.CleanPrefix;

        public static string CleanSuffix => _parsed.CleanSuffix;

        /// <summary>Gets the exact display name of the given user's watchlist collection, or null if the feature is off.</summary>
        public static string GetNameForUser(string username)
        {
            if (!_parsed.IsValid || string.IsNullOrEmpty(username))
            {
                return null;
            }

            return _parsed.Prefix + username + _parsed.Suffix;
        }

        private static ParsedTemplate Parse()
        {
            var raw = Environment.GetEnvironmentVariable("JELLYFIN_OWN_WATCHLIST_NAME_TEMPLATE");
            if (string.IsNullOrEmpty(raw))
            {
                return ParsedTemplate.Invalid;
            }

            var index = raw.IndexOf(Placeholder, StringComparison.Ordinal);
            if (index < 0)
            {
                return ParsedTemplate.Invalid;
            }

            var prefix = raw[..index];
            var suffix = raw[(index + Placeholder.Length)..];

            var cleanedWithSentinel = raw.Replace(Placeholder, Sentinel, StringComparison.Ordinal).GetCleanValue() ?? string.Empty;
            var sentinelIndex = cleanedWithSentinel.IndexOf(Sentinel, StringComparison.Ordinal);

            string cleanPrefix;
            string cleanSuffix;
            if (sentinelIndex < 0)
            {
                // Sentinel itself got fully cleaned away (shouldn't happen - it's all lowercase
                // letters - but fall back to the independent-clean approach rather than crash).
                cleanPrefix = prefix.GetCleanValue() ?? string.Empty;
                cleanSuffix = suffix.GetCleanValue() ?? string.Empty;
            }
            else
            {
                cleanPrefix = cleanedWithSentinel[..sentinelIndex];
                cleanSuffix = cleanedWithSentinel[(sentinelIndex + Sentinel.Length)..];
            }

            if (cleanPrefix.Length == 0 && cleanSuffix.Length == 0)
            {
                // No fixed literal text anywhere in the template - a SQL prefix/suffix match would
                // match every BoxSet in the library. Refuse rather than hide the entire Collections view.
                return ParsedTemplate.Invalid;
            }

            return new ParsedTemplate(prefix, suffix, cleanPrefix, cleanSuffix, true);
        }

        private readonly record struct ParsedTemplate(string Prefix, string Suffix, string CleanPrefix, string CleanSuffix, bool IsValid)
        {
            public static readonly ParsedTemplate Invalid = new(string.Empty, string.Empty, string.Empty, string.Empty, false);
        }
    }
}
