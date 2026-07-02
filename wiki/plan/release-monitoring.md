# Release Monitoring

Use this page to check whether the course has new videos or the companion repo has new code.

## Sources

- Playlist: https://www.youtube.com/playlist?list=PLhGl0l5La4sZDg1isoXWnFGXBXDyM42sQ
- Playlist RSS: https://www.youtube.com/feeds/videos.xml?playlist_id=PLhGl0l5La4sZDg1isoXWnFGXBXDyM42sQ
- Companion repo: https://github.com/rwjdk/chatbot

## Check For New Videos

Run this from any shell with network access:

```bash
python3 - <<'PY'
import urllib.request, xml.etree.ElementTree as ET
url = 'https://www.youtube.com/feeds/videos.xml?playlist_id=PLhGl0l5La4sZDg1isoXWnFGXBXDyM42sQ'
root = ET.fromstring(urllib.request.urlopen(url, timeout=30).read())
ns = {'a': 'http://www.w3.org/2005/Atom', 'yt': 'http://www.youtube.com/xml/schemas/2015'}
print(root.findtext('a:title', namespaces=ns))
for entry in root.findall('a:entry', ns):
    print(entry.findtext('a:published', namespaces=ns), entry.findtext('a:title', namespaces=ns), entry.findtext('yt:videoId', namespaces=ns))
PY
```

Then compare the output with [[Course Playlist Tracker]].

If a new video exists:

1. Add it to `wiki/plan/course-playlist-tracker.md`.
2. Create a new note under `wiki/plan/video-notes/` using kebab-case naming.
3. Add it to [[Video Implementation Roadmap]].
4. Add an entry to [[Log]].
5. Commit the wiki update before implementation if it materially changes the plan.

## Check For New Companion Repo Code

Keep a local clone of the companion repo outside this repository. Example:

```bash
mkdir -p ~/source/reference
cd ~/source/reference
git clone https://github.com/rwjdk/chatbot.git rwjdk-chatbot
```

For later checks:

```bash
COMPANION_REPO=~/source/reference/rwjdk-chatbot
git -C "$COMPANION_REPO" fetch --all --tags --prune
git -C "$COMPANION_REPO" log --oneline HEAD..origin/main
git -C "$COMPANION_REPO" diff --stat HEAD..origin/main
```

If new commits exist:

1. Read the commit messages and changed files.
2. Pull the reference clone with `git -C "$COMPANION_REPO" pull --ff-only`.
3. Update [[Course Playlist Tracker]] with the latest companion repo state.
4. Update [[Video Implementation Roadmap]] if new commits clarify or change the mapping.
5. Update the relevant note under `wiki/plan/video-notes/`.
6. If HomeScout should intentionally differ from the companion code, record it in [[Plan Divergence]].
7. Add an entry to [[Log]].

## Quick Combined Checklist

Use this before starting a new course session:

- [ ] Check playlist RSS for new videos.
- [ ] Check companion repo for new commits.
- [ ] Update [[Course Playlist Tracker]].
- [ ] Update [[Video Implementation Roadmap]] if mapping changed.
- [ ] Update or create the relevant video note.
- [ ] Record intentional differences in [[Plan Divergence]].
- [ ] Append the session to [[Log]].

## What Not To Do

- Do not copy companion code blindly.
- Do not maintain a separate `docs/` plan copy.
- Do not change `wiki/raw/`; create a new raw source file if a source snapshot needs to be preserved.
- Do not implement a newly released feature until the wiki mapping has been updated.
