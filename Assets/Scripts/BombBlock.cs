using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombBlock : Block
{
    public override void Change()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
    }
    protected override int GetID()
    {
        return -1;
    }

    protected override void OnMouseDown()
    {
        if (GameManager.Instance.IsLockBlock)
            return;

        GameManager.Instance.UseBomb(this);
    }
    protected override void OnMouseDrag()
    {
    }
    protected override void OnMouseUp()
    {
    }
}
