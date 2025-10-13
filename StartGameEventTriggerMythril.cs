using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.EventSystems;

namespace MythrilSoul
{
    internal class StartGameEventTriggerMythril : MenuButtonListCondition, ISubmitHandler, IEventSystemHandler, IPointerClickHandler
    {
        public bool permaDeath;

        public bool bossRush;

        public Action preSubmit;

        public void OnSubmit(BaseEventData eventData)
        {
            preSubmit?.Invoke();
            UIManager.instance.StartNewGame(permaDeath, bossRush);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnSubmit(eventData);
        }

        public override bool IsFulfilled()
        {
            bool result = true;
            if (permaDeath && GameManager.instance.GetStatusRecordInt("RecPermadeathMode") == 0)
            {
                result = false;
            }

            if (bossRush && GameManager.instance.GetStatusRecordInt("RecBossRushMode") == 0)
            {
                result = false;
            }

            return result;
        }
    }
}
