using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PDR
{
    public struct SkillInfo
    {
        public int cost;
        public float value;
        public string spritePath;
    }

    public class PlayerState : MapPawn
    {
        public float _health;
        public float _maxHealth;
        public float _attack;
        public float _defence;
        public int _experience;
        public int _upgradeExperience;
        public int _level;

        public List<SkillInfo> skillInfos;

        public PlayerState(float health, float attack, float defence, int upgradeExperience, BlockInfo blockInfo)
        {
            _health = health - 50;
            _maxHealth = health;
            _attack = attack;
            _defence = defence;
            _experience = 0;
            _upgradeExperience = upgradeExperience;
            _level = 1;
            _blockInfo = blockInfo;
            skillInfos = new List<SkillInfo>();
            skillInfos.Add(new SkillInfo()
            {
                cost = 1,
                value = attack,
                spritePath = "attack"
            });
            skillInfos.Add(new SkillInfo()
            {
                cost = 1,
                value = defence,
                spritePath = "defence"
            });
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

        public override void UpateBlockInfo(BlockInfo block)
        {
            base.UpateBlockInfo(block);
            _blockInfo = block;
        }
    }
}
