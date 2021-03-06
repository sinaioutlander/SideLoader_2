﻿namespace SideLoader
{
    public class SL_Cough : SL_Effect
    {
        public int ChanceToTrigger;

        public override void ApplyToComponent<T>(T component)
        {
            (component as Cough).ChancesToTrigger = this.ChanceToTrigger;
        }

        public override void SerializeEffect<T>(T effect)
        {
            ChanceToTrigger = (effect as Cough).ChancesToTrigger;
        }
    }
}
