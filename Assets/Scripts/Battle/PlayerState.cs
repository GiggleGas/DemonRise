using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PDR
{
    public class PlayerState
    {
        public int _health;
        public int _maxHealth;
        public int _attack;
        public int _experience;
        public int _upgradeExperience;
        public int _level;
        public BlockInfo _blockInfo;

        public PlayerState(int health, int attack, int upgradeExperience, BlockInfo blockInfo)
        {
            _health = health;
            _maxHealth = health;
            _attack = attack;
            _experience = 0;
            _upgradeExperience = upgradeExperience;
            _level = 1;
            _blockInfo = blockInfo;
        }

        public bool TryUpdate()
        {
            if(_experience < _upgradeExperience)
            {
                return false;
            }
            _experience -= _upgradeExperience;
            return true;
        }
    }
}
