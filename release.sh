#!/usr/bin/env sh
# Cuts a release: regenerates the changelog, commits it, and tags the commit.
#
#   ./release.sh 1.2.0-alpha
#
# The changelog has to live inside the tagged commit (the Docker build has no
# git history), so the generator is told which version the not-yet-tagged
# commits belong to, and the tag is created afterwards.

set -e

version="$1"

if [ -z "$version" ]; then
	echo "usage: ./release.sh <version>   e.g. ./release.sh 1.2.0-alpha" >&2
	exit 1
fi

tag="v${version#v}"

if [ -n "$(git status --porcelain)" ]; then
	echo "✖ working tree is not clean; commit or stash first" >&2
	exit 1
fi

if git rev-parse -q --verify "refs/tags/$tag" >/dev/null; then
	echo "✖ tag $tag already exists" >&2
	exit 1
fi

dotnet run scripts/generate-changelog.cs -- "$version"

git add src/Noo.Api/Platform/changelog.json
git commit -m "chore(release): $tag"
git tag "$tag"

# Pushed explicitly rather than with --follow-tags, which only picks up
# annotated tags and would silently push the commit without this one.
git push origin HEAD
git push origin "$tag"

echo "✔ released $tag"
