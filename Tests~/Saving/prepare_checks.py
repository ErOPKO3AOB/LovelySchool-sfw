"""Build an isolated Unity project for native save regression checks."""
from pathlib import Path
import argparse
import shutil
import tempfile

args = argparse.ArgumentParser()
args.add_argument('--package', required=True, help='Path to the installed UniWebKit package')
args.add_argument('--game', help='Optional game root; defaults to the repository containing this test')
args.add_argument('--output', help='Empty output directory; defaults to a new temporary directory')
opts = args.parse_args()
test = Path(__file__).resolve().parent
game = Path(opts.game).resolve() if opts.game else test.parents[1]
package = Path(opts.package).resolve()
project = Path(opts.output).resolve() if opts.output else Path(tempfile.mkdtemp(prefix='LovelySchoolSaveChecks_'))
if project.exists() and any(project.iterdir()):
    raise RuntimeError('Output directory must be empty')
assets = project / 'Assets'
assets.mkdir(parents=True, exist_ok=True)
for source in (package / 'Runtime/Features/Player').glob('*.cs'):
    shutil.copyfile(source, assets / source.name)
for relative in ['Runtime/Features/IUniWebFeature.cs', 'Runtime/Providers/VKGames/VKGamesPlayerProvider.cs']:
    source = package / relative
    shutil.copyfile(source, assets / source.name)
for relative in ['Game/GameSaveSession.cs', 'Game/InventorySaveData.cs', 'Player/Inventory.cs']:
    source = game / 'Assets/Scripts' / relative
    shutil.copyfile(source, assets / source.name)
weapon = (game / 'Assets/Scripts/Weapons/Weapon.cs').read_text(encoding='utf-8-sig')
(assets / 'WeaponData.cs').write_text('namespace Game {\n' + weapon[weapon.index('    [System.Serializable]'):], encoding='utf-8')
(assets / 'Bridge.cs').write_text('namespace UniWebKit { public static class UniWebBridge { public static IPlayerProvider Player; } }', encoding='utf-8')
(assets / 'csc.rsp').write_text('-define:UNIWEB_VK_GAMES\n', encoding='utf-8')
shutil.copyfile(test / 'GameplayDoubles.cs', assets / 'GameplayDoubles.cs')
editor = assets / 'Editor'
editor.mkdir()
shutil.copyfile(test / 'NativeChecks.cs', editor / 'LovelySchoolSaveChecks.cs')
settings = project / 'ProjectSettings'
settings.mkdir()
(settings / 'ProjectVersion.txt').write_text((game / 'ProjectSettings/ProjectVersion.txt').read_text(), encoding='utf-8')
packages = project / 'Packages'
packages.mkdir()
(packages / 'manifest.json').write_text('{"dependencies":{}}', encoding='utf-8')
print(project)
