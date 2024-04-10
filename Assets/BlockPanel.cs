using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum VECTOR
{
    UP,
    DOWN,
    LEFT,
    RIGHT,
}

public class BlockPanel : MonoBehaviour
{
    private class BlockGroup
    {
        public Block[] blocks;      // 연결된 블록 배열.
        public bool isHorizontal;   // 가로인지 세로인지.

        public int Length => blocks.Length;
    }

    public static BlockPanel Instance { get; private set; }

    const int BLOCK_WIDTH = 6;
    const int BLOCK_HEIGHT = 6;

    [SerializeField] Block blockPrefab;

    public bool IsLockBlock;
    Block[] blocks;

    private void Awake()
    {
        Instance = this;
    }
    private void Start()
    {
        GenerateBlock();
    }

    [ContextMenu("새로운 블록 만들기")]
    public void GenerateBlock()
    {
        // 블록 배열이 없으면 개수에 맞춰 생성한다.
        if (blocks == null)
            blocks = new Block[BLOCK_WIDTH * BLOCK_HEIGHT];

        // 블록들을 생성하기 전 기존 블록들을 삭제
        for (int i = 0; i < blocks.Length; i++)
        {
            if (blocks[i] != null)
                DestroyImmediate(blocks[i].gameObject);
        }

        // 배열에 저장되지 않은 블록을 찾아 에디터 상에서 삭제
        Block[] anotherBlocks = FindObjectsOfType<Block>();
        foreach (Block block in anotherBlocks)
            DestroyImmediate(block.gameObject);

        // 블록 생성.
        Vector3 pivot = new Vector3((BLOCK_WIDTH / 2 - 0.5f) * -1f, BLOCK_HEIGHT / 2 - 0.5f, 0);
        for (int i = 0; i < blocks.Length; i++)
        {
            // 블록을 새로 생성한 뒤 정보를 대입한다.
            Block block = Instantiate(blockPrefab, transform);
            block.Setup(i, i % BLOCK_WIDTH, i / BLOCK_WIDTH);

            // 블록의 위치와 크기를 설정한다.
            block.transform.localPosition = pivot + new Vector3(block.info.x, -block.info.y);
            block.transform.localScale = Vector3.one;
            blocks[i] = block;
        }

        // 중복 블록 체크.
        foreach (Block block in blocks)
        {
            for (int i = 0; i < 2; i++)
            {
                while (true)
                {
                    BlockGroup group = SearchGroup(block, i == 0);
                    if (group.Length >= 3)
                    {
                        block.Change();
                    }
                    else
                        break;
                }
            }
        }
    }

    // 연결된 블록 가져오기
    private BlockGroup SearchGroup(Block block, bool isHorizontal)
    {
        // 최소, 최대 인덱스와 더해가는 값.
        int minIndex = isHorizontal ? block.info.y * BLOCK_WIDTH : block.info.x;
        int maxIndex = isHorizontal ? minIndex + BLOCK_WIDTH - 1 : minIndex + (BLOCK_WIDTH * (BLOCK_HEIGHT - 1));
        int addValue = isHorizontal ? 1 : BLOCK_WIDTH;

        List<Block> result = new List<Block>();
        for (int index = minIndex; index <= maxIndex; index += addValue)
        {
            List<Block> check = new List<Block>();
            for (int i = index; i <= maxIndex; i += addValue)
            {
                if (block.id == blocks[i].id)
                    check.Add(blocks[i]);
                else
                    break;
            }

            if (check.Contains(block) && check.Count > result.Count)
                result = check;
        }

        // 결과를 BlockGroup으로 반환.
        BlockGroup group = new BlockGroup();
        group.blocks = result.ToArray();
        group.isHorizontal = isHorizontal;

        return group;
    }
    private bool MatchBlockAfterSwap(params Block[] matechBlocks)
    {
        List<Block> removeList = new List<Block>();
        foreach(Block block in matechBlocks)
        {
            BlockGroup horizontalGroup = SearchGroup(block, true);
            BlockGroup verticalGroup = SearchGroup(block, false);

            if(horizontalGroup.Length >= 3)
            {
                removeList.AddRange(horizontalGroup.blocks);
            }
            if(verticalGroup.Length >= 3)
            {
                removeList.AddRange(verticalGroup.blocks);
            }
        }

        // 제거할 블록이 최소 3개 이상인 경우 매치 성공.
        bool isMatch = removeList.Count >= 3;
        if(isMatch)
        {
            // IEnumerable.Distinct() : 중복 제거.
            foreach (Block removeBlock in removeList.Distinct())
            {
                blocks[removeBlock.info.index] = null;  // 배열에서 삭제.
                Destroy(removeBlock.gameObject);        // 실제 게임 오브젝트 제거.
            }
        }
        return isMatch;
    }
    public bool SwapBlock(int index, VECTOR dir)
    {
        int targetIndex = index;
        targetIndex += dir switch
        {
            VECTOR.UP => -BLOCK_WIDTH,
            VECTOR.DOWN => BLOCK_WIDTH,
            VECTOR.LEFT => -1,
            VECTOR.RIGHT => 1,
            _ => 0
        };

        // 범위를 벗어나는 경우.
        if (targetIndex < 0 || targetIndex >= blocks.Length)
            return false;

        // 같은 라인이 아닌 경우.
        if (dir == VECTOR.LEFT || dir == VECTOR.RIGHT)
        {
            if (blocks[index].info.y != blocks[targetIndex].info.y)
                return false;
        }

        StartCoroutine(IECheckSwap(index, targetIndex));
        return true;
    }
    
    IEnumerator IECheckSwap(int index, int targetIndex)
    {
        IsLockBlock = true;
        Block block = blocks[index];
        Block targetBlock = blocks[targetIndex];
        yield return StartCoroutine(IESwap(block, targetBlock));        // 스왑이 끝날때까지 대기한다.

        // 매치가 되지 않았다면 다시 스왑을 한다.
        if(MatchBlockAfterSwap(block, targetBlock) == false)
            yield return StartCoroutine(IESwap(block, targetBlock));

        IsLockBlock = false;
    }
    IEnumerator IESwap(Block blockA, Block blockB)
    {
        const float SWAP_SPEED = 5f;

        // 교환
        Vector3 posA = blockB.transform.localPosition;
        Vector3 posB = blockA.transform.localPosition;
        while (blockA.transform.localPosition != posA || blockB.transform.localPosition != posB)
        {
            blockA.transform.localPosition = Vector3.MoveTowards(blockA.transform.localPosition, posA, SWAP_SPEED * Time.deltaTime);
            blockB.transform.localPosition = Vector3.MoveTowards(blockB.transform.localPosition, posB, SWAP_SPEED * Time.deltaTime);
            yield return null;
        }

        // blocks 배열 내부 교환.
        int indexA = blockA.info.index;
        int indexB = blockB.info.index;
        blocks[indexA] = blockB;
        blocks[indexB] = blockA;

        // block끼리 info 교환.
        Block.Info tempInfo = blockA.info;
        blockA.info = blockB.info;
        blockB.info = tempInfo;

        blockA.name = blockA.info.ToString();
        blockB.name = blockB.info.ToString();
    }
}
