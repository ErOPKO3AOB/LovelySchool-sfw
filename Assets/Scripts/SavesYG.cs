using Game;
using System.Collections.Generic;

namespace YG
{
    public partial class SavesYG
    {
        public int idSave = 0;

        public int currentCheckPoint;

        public List<WeaponData> weaponDatas = new();

        public int gameCompletedTimes = 0;
    }
}