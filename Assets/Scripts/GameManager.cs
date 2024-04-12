using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Unity.Collections.AllocatorManager;

public enum VECTOR
{
    UP,
    DOWN,
    LEFT,
    RIGHT,
}

public class GameManager : MonoBehaviour
{
    private class BlockGroup
    {
        public Block[] blocks;      // ����� ��� �迭.
        public bool isHorizontal;   // �������� ��������.

        public int Length => blocks.Length;
    }

    static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<GameManager>();
            return instance;
        }
    }

    const int BLOCK_WIDTH = 7;
    const int BLOCK_HEIGHT = 7;

    [SerializeField] Block blockPrefab;
    [SerializeField] BombBlock bombBlockPrefab;
    [SerializeField] GameUI gameUI;
    [SerializeField] AudioPlayer audioPlayer;
    [SerializeField] ParticleSystem popFxPrefab;
    [SerializeField] LeaderBoard leaderBoard;
    [SerializeField] Sprite[] animalSprites;


    Block[] panelBlocks;

    const float MAX_TIME = 60f;     // 1��.
    float currentTime;              // ���� �ð�.
    int score;                      // ����.

    const float MAX_BOMB = 200;     // �ִ� ��ź ������.
    float currentBomb;              // ���� ��ź ������.
    bool createBomb;                // ��ź ���� ����.

    const float MAX_COMBO_TIME = 3f;    // �޺� �ð�.
    float comboTime;                    // �޺� �ð�.   
    int combo;                          // �޺� Ƚ��.

    bool isLockTime;                    // �ð��� �帧 ����.
    bool isLockBlock;                   // ���� ��Ʈ�� ����.
    bool isGameOver;                    // ���� ���� ����.

    public bool IsLockBlock => isLockBlock || isGameOver;
    public int Combo => combo;

    private void Awake()
    {
        instance = this;
    }
    private void Start()
    {
        GenerateBlock();

        currentTime = MAX_TIME;
        isLockBlock = true;
        isLockTime = true;
        isGameOver = false;

    }
    public void OnStartGame()
    {
        StartCoroutine(IEStartGame());
    }
    private IEnumerator IEStartGame()
    {
        float timer = 3f;
        while (timer > 0.0f)
        {
            timer -= Time.deltaTime;
            gameUI.UpdateStartTimer(timer);
            yield return null;
        }
        gameUI.UpdateStartTimer(0);

        audioPlayer.SwitchBGM(true);
        isLockBlock = false;
        isLockTime = false;
    }



    private void Update()
    {
        if (!isGameOver)
        {
            if (!isLockTime)
            {
                currentTime = Mathf.Clamp(currentTime - Time.deltaTime, 0.0f, MAX_TIME);
                comboTime = Mathf.Clamp(comboTime - Time.deltaTime, 0.0f, MAX_COMBO_TIME);
            }

            // UI ����.
            gameUI.UpdateBomb(currentBomb, MAX_BOMB);
            gameUI.UpdateTimer(currentTime, MAX_TIME);
            gameUI.UpdateScore(score);

            // ���� ���� üũ.
            combo = comboTime <= 0.0f ? 0 : combo;
            isGameOver = currentTime <= 0.0f;
            if (isGameOver)
                StartCoroutine(GameOver());
        }
    }


    [ContextMenu("���ο� ��� �����")]
    public void GenerateBlock()
    {
        // ��� �迭�� ������ ������ ���� �����Ѵ�.
        panelBlocks = new Block[BLOCK_WIDTH * BLOCK_HEIGHT];

        // ��ϵ��� �����ϱ� �� ���� ��ϵ��� ����
        for (int i = 0; i < panelBlocks.Length; i++)
        {
            if (panelBlocks[i] != null)
                DestroyImmediate(panelBlocks[i].gameObject);
        }

        // �迭�� ������� ���� ����� ã�� ������ �󿡼� ����
        Block[] anotherBlocks = FindObjectsOfType<Block>();
        foreach (Block block in anotherBlocks)
            DestroyImmediate(block.gameObject);

        // ��� ����.
        for (int i = 0; i < panelBlocks.Length; i++)
            panelBlocks[i] = CreateNewBlock(i);

        // �ߺ� ��� üũ.
        foreach (Block block in panelBlocks)
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

    private IEnumerator GameOver()
    {
        gameUI.UpdatePopup("GAME OVER");
        yield return new WaitForSeconds(2.0f);
        int rank = leaderBoard.AddScore(score);
        leaderBoard.SwitchScore(true, rank);
    }

    // ����� ��� ��������
    private BlockGroup SearchGroup(Block block, bool isHorizontal)
    {
        List<Block> result = new List<Block>();
        if (block != null)
        {
            // �ּ�, �ִ� �ε����� ���ذ��� ��.
            int minIndex = isHorizontal ? block.info.y * BLOCK_WIDTH : block.info.x;
            int maxIndex = isHorizontal ? minIndex + BLOCK_WIDTH - 1 : minIndex + (BLOCK_WIDTH * (BLOCK_HEIGHT - 1));
            int addValue = isHorizontal ? 1 : BLOCK_WIDTH;

            for (int index = minIndex; index <= maxIndex; index += addValue)
            {
                List<Block> check = new List<Block>();
                for (int i = index; i <= maxIndex; i += addValue)
                {
                    if (panelBlocks[i] == null || !block.MatchBlock(panelBlocks[i]))
                        break;

                    check.Add(panelBlocks[i]);
                }

                if (check.Contains(block) && check.Count > result.Count)
                    result = check;
            }
        }

        // ����� BlockGroup���� ��ȯ.
        BlockGroup group = new BlockGroup();
        group.blocks = result.ToArray();
        group.isHorizontal = isHorizontal;

        return group;
    }
    private Block[] MatchBlocks(params Block[] slideBlocks)
    {
        // �Ű������� �ش��ϴ� ��ϵ��� ���ҵ� ����̴�.
        // �ش� ����� �������� ��Ī�� �Ǵ�(=���ӵǴ� ������ 3�� �̻���)����� ã�� �����Ѵ�.
        List<Block> removeList = new List<Block>();
        foreach (Block block in slideBlocks)
        {
            BlockGroup horizontalGroup = SearchGroup(block, true);
            BlockGroup verticalGroup = SearchGroup(block, false);

            if (horizontalGroup.Length >= 3)
            {
                removeList.AddRange(horizontalGroup.blocks);
            }
            if (verticalGroup.Length >= 3)
            {
                removeList.AddRange(verticalGroup.blocks);
            }
        }

        // ������ ����� �ּ� 3�� �̻��� ��� ��ġ ����.
        return removeList.Distinct().ToArray();
    }
    private Vector3 IndexToLocalPos(int index)
    {
        Vector3 pivot = new Vector3((BLOCK_WIDTH / 2) * -1f, BLOCK_HEIGHT / 2, 0);
        int x = index % BLOCK_WIDTH;
        int y = index / BLOCK_WIDTH;
        return pivot + new Vector3(x, -y);
    }

    public Sprite GetSprite(ANIMAL id)
    {
        return animalSprites[(int)id];
    }




    // ��ź ���
    public void UseBomb(Block block)
    {
        StartCoroutine(IEUseBomb(block));
    }
    public IEnumerator IEUseBomb(Block block)
    {
        isLockBlock = true;
        isLockTime = true;

        yield return StartCoroutine(IERemoveBlockAtferBomb(block));
        yield return StartCoroutine(IEMediate());

        // ���ĺ��ʹ� ��� ����� ������� ��Ī���Ѻ���.
        int continuously = 0;
        while (true)
        {
            Block[] matchBlocks = MatchBlocks(panelBlocks);
            if (matchBlocks.Length < 3)
                break;

            yield return StartCoroutine(IERemovePanelBlock(matchBlocks, continuously++));
            yield return StartCoroutine(IEMediate());
        }

        isLockTime = false;
        isLockBlock = false;
    }


    // ��� ��ȯ �Լ�.
    public void SlideBlock(int index, VECTOR dir)
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

        // ������ ����� ���.
        if (targetIndex < 0 || targetIndex >= panelBlocks.Length || panelBlocks[targetIndex] == null)
            return;

        // ���� ������ �ƴ� ���.
        if (dir == VECTOR.LEFT || dir == VECTOR.RIGHT)
        {
            if (panelBlocks[index].info.y != panelBlocks[targetIndex].info.y)
                return;
        }

        StartCoroutine(IESlideBlock(index, targetIndex));
        return;
    }
    IEnumerator IESlideBlock(int index, int targetIndex)
    {
        isLockBlock = true;

        Block block = panelBlocks[index];
        Block targetBlock = panelBlocks[targetIndex];

        yield return StartCoroutine(IEExchangeBlock(block, targetBlock));        // ������ ���������� ����Ѵ�.

        // ��ġ�� �Ǿ��ٸ� ����� ������ �� �� ����� ä���.
        Block[] matchBlocks = MatchBlocks(block, targetBlock);
        

        if (matchBlocks.Length >= 3)
        { 
            isLockTime = true;
            int continuously = 0;

            yield return StartCoroutine(IERemovePanelBlock(matchBlocks, continuously++));
            yield return StartCoroutine(IEMediate());

            // ���ĺ��ʹ� ��� ����� ������� ��Ī���Ѻ���.
            while (true)
            {
                matchBlocks = MatchBlocks(panelBlocks);
                if(matchBlocks.Length < 3)
                    break;

                yield return StartCoroutine(IERemovePanelBlock(matchBlocks, continuously++));
                yield return StartCoroutine(IEMediate());
            }
        }
        else
            yield return StartCoroutine(IEExchangeBlock(block, targetBlock));

        isLockTime = false;
        isLockBlock = false;
    }
    IEnumerator IEExchangeBlock(Block blockA, Block blockB)
    {
        const float SWAP_SPEED = 8f;

        // ������ ��ġ�� ��ȯ�Ѵ�.
        Vector3 posA = blockB.transform.localPosition;
        Vector3 posB = blockA.transform.localPosition;
        while (blockA.transform.localPosition != posA || blockB.transform.localPosition != posB)
        {
            blockA.transform.localPosition = Vector3.MoveTowards(blockA.transform.localPosition, posA, SWAP_SPEED * Time.deltaTime);
            blockB.transform.localPosition = Vector3.MoveTowards(blockB.transform.localPosition, posB, SWAP_SPEED * Time.deltaTime);
            yield return null;
        }

        SwapBlock(blockA.info.index, blockB.info.index);
    }

    // ��� ���� �Լ�.
    IEnumerator IERemoveBlockAtferBomb(Block block)
    {
        yield return new WaitForSeconds(0.2f);

        int top = block.info.x;
        int index = top;

        // �������� ��� ����
        while (index < panelBlocks.Length)
        {
            StartCoroutine(IERemoveBlock(panelBlocks[index]));
            yield return new WaitForSeconds(0.12f);
            index += BLOCK_WIDTH;
        }

        CalculateAfterBlock(BLOCK_WIDTH + BLOCK_HEIGHT - 1, 0, true);

        int bottom = top + (BLOCK_WIDTH * (BLOCK_HEIGHT - 1));
        int min = BLOCK_WIDTH * (BLOCK_HEIGHT - 1);
        int max = BLOCK_WIDTH * BLOCK_HEIGHT - 1;
        int left = bottom - 1;
        int right = bottom + 1;

        // ���� �̵�.
        while (left >= min || max >= right)
        {
            if (left >= min)
            {
                StartCoroutine(IERemoveBlock(panelBlocks[left]));
                left -= 1;
            }
            if (right <= max)
            {
                StartCoroutine(IERemoveBlock(panelBlocks[right]));
                right += 1;
            }

            yield return new WaitForSeconds(0.17f);
        }
    }
    IEnumerator IERemovePanelBlock(Block[] removeBlocks, int continuously)
    {
        // ���� ���, ���ھ� �� ��ź ������ ���.
        audioPlayer.PopBlock();
        CalculateAfterBlock(removeBlocks.Length, continuously, false);

        foreach (Block block in removeBlocks)
            StartCoroutine(IERemoveBlock(block));

        yield return new WaitForSeconds(0.2f);
    }
    IEnumerator IERemoveBlock(Block block)
    {
        Instantiate(popFxPrefab, block.transform.position, Quaternion.identity);
        yield return new WaitForSeconds(0.15f);
        panelBlocks[block.info.index] = null;
        Destroy(block.gameObject);
    }

    // ���� ���.s
    void CalculateAfterBlock(int removeLength, int continuously, bool useBomb)
    {
        // �޺� �ð� ����.
        if (comboTime > 0f)
            combo += 1;
        comboTime = MAX_COMBO_TIME;

        // �������� ��Ʈ�� �� 20%�� ���ʽ�, �޺��� 5%�� ���ʽ�
        int addBlock = removeLength - 3;
        float comboBonus = (1 + ((combo - 1) * 0.05f));
        float continueBonus = (1 + (continuously * 0.2f));

        // 3��� 250 > ���� 1��� �߰����� 100�� �߰�, �޺��� ���ʽ� ���� +20%
        float addScore = (250 + (100 * addBlock)) * (comboBonus + continueBonus);
        score += Mathf.RoundToInt(addScore);

        // ��ź �� ���.
        if (!useBomb)
        {
            // 3��� 10 > ���� 1��� �߰����� 5�� �߰�
            float addBomb = (10 + (5 * addBlock)) * (comboBonus + continueBonus);
            currentBomb += Mathf.RoundToInt(addBomb);
            if (currentBomb >= MAX_BOMB)
            {
                currentBomb %= MAX_BOMB;
                createBomb = true;
            }
        }
    }

   
    // ��� �����ϱ�.
    IEnumerator IEMediate()
    {
        MediateEmptySpace();        // MatchBlockToRemove()�� ���� �� ������ �߻����� ��� ����� ������.
        FillNewBlock();             // �� ������ ä���.

        const float FALL_SPEED = 8f;

        // ���ŵ� �ڽ��� ��ġ�� �̵��Ѵ�.
        int movingCount = 0;
        do
        {
            movingCount = 0;
            foreach (Block block in panelBlocks)
            {
                if (block == null)
                    continue;

                Vector3 destination = IndexToLocalPos(block.info.index);
                if (block.transform.localPosition != destination)
                {
                    movingCount++;
                    block.transform.localPosition = Vector3.MoveTowards(block.transform.localPosition, destination, FALL_SPEED * Time.deltaTime);
                }
            }
            yield return null;

        } while (movingCount > 0);
    }
    private void MediateEmptySpace()
    {
        // ������ ��Ϻ��� �������� �ڽ��� �Ʒ��� �� ����� �ִٸ� ������ �����Ѵ�.
        foreach (Block block in panelBlocks.Reverse())
        {
            if (block == null)
                continue;

            int nextIndex = block.info.index;
            while (true)
            {
                nextIndex += BLOCK_WIDTH;
                if (nextIndex >= panelBlocks.Length || panelBlocks[nextIndex] != null)
                    break;

                SwapBlock(block.info.index, nextIndex);
            }
        }
    }
    private void FillNewBlock()
    {
        // �� ��Ͽ� ���ο� ����� ������ �� ��ġ�� �����Ѵ�. (=�Ƹ� ȭ�� �� �������� �̵�)

        // �� x�� ���κ��� �� ����� ������ ����Ѵ�.
        int[] emptyCounts = new int[BLOCK_WIDTH];
        for(int index = 0; index < panelBlocks.Length; index++)
        {
            if (panelBlocks[index] == null)
                emptyCounts[index % BLOCK_WIDTH] += 1;
        }

        // ��� �� ����� �������� ���ο� ����� �����Ѵ�.
        var emptyIndex = panelBlocks.Select((block, index) => new { block, index })
                                    .Where(pair => pair.block == null)
                                    .Select(pair => pair.index)
                                    .ToList();
        int bombIndex = -1;
        if (createBomb)
        {
            bombIndex = emptyIndex.ElementAt(Random.Range(0, emptyIndex.Count()));
            createBomb = false;
        }

        // ��� �� �ε����� ���鼭 ���ο� ��� ����.
        foreach(int index in emptyIndex)
        {
            if (panelBlocks[index] != null)
                continue;

            // ������ �� ����� ���� x�� ������ �� ������ ���� ��ġ�� �ø���.
            Block newBlock = CreateNewBlock(index, index == bombIndex);
            newBlock.transform.localPosition += Vector3.up * emptyCounts[index % BLOCK_WIDTH];
            panelBlocks[index] = newBlock;
        }
    }

    // ��� ���� �� ��ȯ�ϱ�.
    private Block CreateNewBlock(int index, bool isBomb = false)
    {
        Block prefab = isBomb ? bombBlockPrefab : blockPrefab;
        Block block = Instantiate(prefab, transform);
        block.Setup(index, index % BLOCK_WIDTH, index / BLOCK_WIDTH);

        // ����� ��ġ�� ũ�⸦ �����Ѵ�.
        block.transform.localScale = new Vector3(1 / transform.localScale.x, 1 / transform.localScale.y, 1 / transform.localScale.z);
        block.transform.localPosition = IndexToLocalPos(index);
        return block;
    }
    private void SwapBlock(int indexA, int indexB)
    {
        Block blockA = panelBlocks[indexA];
        Block blockB = panelBlocks[indexB];

        // blocks �迭 ���� ��ȯ.
        panelBlocks[indexA] = blockB;
        panelBlocks[indexB] = blockA;

        // block���� info ��ȯ.
        if(blockA != null)
        {
            blockA.info.index = indexB;
            blockA.info.x = indexB % BLOCK_WIDTH;
            blockA.info.y = indexB / BLOCK_WIDTH;
            blockA.name = blockA.info.ToString();
        }
        if(blockB != null)
        {
            blockB.info.index = indexA;
            blockB.info.x = indexA % BLOCK_WIDTH;
            blockB.info.y = indexA / BLOCK_WIDTH;
            blockB.name = blockB.info.ToString();
        }
    }



    public void OnRetryGame()
    {
        SceneManager.LoadScene("Game");
    }

}
