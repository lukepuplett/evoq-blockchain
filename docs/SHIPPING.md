# Shipping Guide for Evoq.Blockchain

This document outlines the complete process for shipping a new release of Evoq.Blockchain to ensure consistency and prevent version mismatches.

## Pre-Release Checklist

Before starting the release process, ensure:

- [ ] All tests pass (`dotnet test`)
- [ ] No critical warnings in build
- [ ] All public APIs are documented
- [ ] No TODO comments remain
- [ ] Changes are committed and pushed to main branch
- [ ] Working directory is clean (`git status`)

## Release Process

### 1. Version Management

**CRITICAL**: Always check current versions before proceeding:

```bash
# Check current project version
grep '<Version>' src/Evoq.Blockchain/Evoq.Blockchain.csproj

# Check latest git tag
git tag --list --sort=-version:refname | head -1

# Check latest NuGet version
curl -s "https://api.nuget.org/v3/registration5-semver1/evoq.blockchain/index.json" | grep -o '"version":"[^"]*"' | tail -1
```

**Version Bump Rules**:
- **PATCH** (1.6.0 → 1.6.1): Bug fixes, no new features
- **MINOR** (1.6.0 → 1.7.0): New features, backward compatible
- **MAJOR** (1.6.0 → 2.0.0): Breaking changes

### 2. Update Version

Update the version in `src/Evoq.Blockchain/Evoq.Blockchain.csproj`:

```xml
<Version>1.8.0</Version>
```

### 3. Update CHANGELOG.md

Add a new entry at the top of `CHANGELOG.md`:

```markdown
## [1.8.0] - 2024-01-XX

### Added
- Selective disclosure functionality with `MerkleTree.From()` methods
- `TryReadJsonKeys()` method on `MerkleLeaf` for JSON key extraction
- V3.0 metadata leaf preservation during selective disclosure
- `NonJsonLeafException` for non-JSON leaf handling

### Fixed
- V3.0 header leaf salt preservation during selective disclosure
- Root hash consistency across selective disclosure operations

### Changed
- Enhanced metadata leaf detection with flexible content type matching
```

### 4. Build and Test

```bash
# Run full build and test suite
./build.sh

# Verify package was created
ls -la artifacts/
```

### 5. Commit Version Changes

```bash
# Commit version and changelog updates
git add .
git commit -m "Bump version to 1.8.0 for selective disclosure release"
git push origin master
```

### 6. Create Git Tag

```bash
# Create annotated tag
git tag -a v1.8.0 -m "Version 1.8.0 - Add selective disclosure with V3.0 metadata preservation"

# Push tag to remote
git push origin v1.8.0
```

### 7. Create GitHub Release

**Option A: Using GitHub CLI (Recommended)**
```bash
# Create release with changelog from file
gh release create v1.8.0 \
  --title "Version 1.8.0 - Selective Disclosure with V3.0 Metadata Preservation" \
  --notes-file CHANGELOG.md

# Or create with inline notes
gh release create v1.8.0 \
  --title "Version 1.8.0 - Selective Disclosure with V3.0 Metadata Preservation" \
  --notes "Added selective disclosure functionality with V3.0 metadata preservation..."
```

**Option B: Using GitHub Web Interface**
1. Go to https://github.com/lukepuplett/evoq-blockchain/releases
2. Click "Create a new release"
3. Select the tag you just pushed (v1.8.0)
4. Set release title: "Version 1.8.0 - Selective Disclosure with V3.0 Metadata Preservation"
5. Copy the changelog content from `CHANGELOG.md`
6. **DO NOT** upload the .nupkg file manually - let the publish script handle it
7. Click "Publish release"

### 8. Publish to NuGet

**Manual Upload to NuGet.org**:
1. Go to https://www.nuget.org/packages/manage/upload
2. Upload the .nupkg file from `./artifacts/Evoq.Blockchain.1.8.0.nupkg`
3. Verify package metadata and click "Submit"

**Alternative: Using publish script** (if automated publishing is preferred):
```bash
# Set your NuGet API key
export NUGET_API_KEY="your-nuget-api-key"

# Publish to NuGet.org
./publish.sh
```

### 9. Verify Release

After publishing, verify:

```bash
# Check NuGet.org has the new version
curl -s "https://api.nuget.org/v3/registration5-semver1/evoq.blockchain/index.json" | grep -o '"version":"[^"]*"' | tail -1

# Should show: "version":"1.8.0"
```

## Common Issues and Solutions

### Version Mismatch
If you see different versions in different places:
1. **Project file**: Check `src/Evoq.Blockchain/Evoq.Blockchain.csproj`
2. **Git tags**: Check `git tag --list`
3. **NuGet.org**: Check the API response above

### Missing Git Tag
If a version was published to NuGet but no git tag exists:
1. Create the missing tag: `git tag -a v1.7.0 -m "Version 1.7.0 - Previous release"`
2. Push the tag: `git push origin v1.7.0`
3. Create a GitHub release for that tag

### Build Failures
If `./build.sh` fails:
1. Check for test failures: `dotnet test`
2. Check for build warnings: `dotnet build --configuration Release`
3. Fix issues before proceeding

## Release Templates

### Git Tag Message Template
```
Version X.Y.Z - Brief description of main features

- Feature 1 description
- Feature 2 description
- Fix 1 description
```

### GitHub Release Title Template
```
Version X.Y.Z - Main Feature Description
```

### Changelog Entry Template
```markdown
## [X.Y.Z] - YYYY-MM-DD

### Added
- New feature 1
- New feature 2

### Fixed
- Bug fix 1
- Bug fix 2

### Changed
- Breaking change 1
- Enhancement 1
```

## Managing Releases with GitHub CLI

### Useful GitHub CLI Commands
```bash
# List all releases
gh release list

# View a specific release
gh release view v1.8.0

# Edit an existing release
gh release edit v1.8.0 --title "New Title" --notes "New notes"

# Create a draft release for review
gh release create v1.8.0 --title "Version 1.8.0" --notes "Notes" --draft

# Upload assets to an existing release
gh release upload v1.8.0 artifacts/Evoq.Blockchain.1.8.0.nupkg
```

### Authentication
Make sure you're authenticated with GitHub CLI:
```bash
# Check authentication status
gh auth status

# Login if needed
gh auth login
```

## Emergency Procedures

### Reverting a Release
If a release needs to be reverted:

1. **DO NOT** delete the git tag (it's immutable)
2. **DO NOT** unpublish from NuGet (it's permanent)
3. Create a new patch release with fixes
4. Document the issue in the changelog

### Hotfix Release
For critical bug fixes:

1. Create a hotfix branch: `git checkout -b hotfix/1.8.1`
2. Make minimal changes to fix the issue
3. Bump version to 1.8.1
4. Follow normal release process
5. Merge hotfix branch back to master

## Automation Ideas

Future improvements could include:
- GitHub Actions for automated releases
- Automated version bumping
- Automated changelog generation
- Automated NuGet publishing
- Release validation checks 