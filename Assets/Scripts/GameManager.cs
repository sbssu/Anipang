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
        public Block[] blocks;      // 연결된 블록 배열.
        public bool isHorizontal;   // 가로인지 세로인지.

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

    const float MAX_TIME = 60f;     // 1분.
    float currentTime;              // 현재 시간.
    int score;                      // 점수.

    const float MAX_BOMB = 200;     // 최대 폭탄 게이지.
    float currentBomb;              // 현재 폭탄 게이지.
    bool createBomb;                // 폭탄 생성 개수.

    const float MAX_COMBO_TIME = 3f;    // 콤보 시간.
    float comboTime;                    // 콤보 시간.   
    int combo;                          // 콤보 횟수.

    bool isLockTime;                    // 시간의 흐름 막기.
    bool isLockBlock;                   // 유저 컨트롤 막기.
    bool isGameOver;                    // 게임 오버 여부.

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

            // UI 갱신.
            gameUI.UpdateBomb(currentBomb, MAX_BOMB);
            gameUI.UpdateTimer(currentTime, MAX_TIME);
            gameUI.UpdateScore(score);

            // 게임 오버 체크.
            combo = comboTime <= 0.0f ? 0 : combo;
            isGameOver = currentTime <= 0.0f;
            if (isGameOver)
                StartCoroutine(GameOver());
        }
    }


    [ContextMenu("새로운 블록 만들기")]
    public void GenerateBlock()
    {
        // 블록 배열이 없으면 개수에 맞춰 생성한다.
        panelBlocks = new Block[BLOCK_WIDTH * BLOCK_HEIGHT];

        // 블록들을 생성하기 전 기존 블록들을 삭제
        for (int i = 0; i < panelBlocks.Length; i++)
        {
            if (panelBlocks[i] != null)
                DestroyImmediate(panelBlocks[i].gameObject);
        }

        // 배열에 저장되지 않은 블록을 찾아 에디터 상에서 삭제
        Block[] anotherBlocks = FindObjectsOfType<Block>();
        foreach (Block block in anotherBlocks)
            DestroyImmediate(block.gameObject);

        // 블록 생성.
        for (int i = 0; i < panelBlocks.Length; i++)
            panelBlocks[i] = CreateNewBlock(i);

        // 중복 블록 체크.
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

    // 연결된 블록 가져오기
    private BlockGroup SearchGroup(Block block, bool isHorizontal)
    {
        List<Block> result = new List<Block>();
        if (block != null)
        {
            // 최소, 최대 인덱스와 더해가는 값.
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

        // 결과를 BlockGroup으로 반환.
        BlockGroup group = new BlockGroup();
        group.blocks = result.ToArray();
        group.isHorizontal = isHorizontal;

        return group;
    }
    private Block[] MatchBlocks(params Block[] slideBlocks)
    {
        // 매개변수에 해당하는 블록들은 스왑된 블록이다.
        // 해당 블록을 기준으로 매칭이 되는(=연속되는 개수가 3개 이상인)블록을 찾아 제거한다.
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

        // 제거할 블록이 최소 3개 이상인 경우 매치 성공.
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




    // 폭탄 사용
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

        // 이후부터는 모든 블록을 대상으로 매칭시켜본다.
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


    // 블록 교환 함수.
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

        // 범위를 벗어나는 경우.
        if (targetIndex < 0 || targetIndex >= panelBlocks.Length || panelBlocks[targetIndex] == null)
            return;

        // 같은 라인이 아닌 경우.
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

        yield return StartCoroutine(IEExchangeBlock(block, targetBlock));        // 스왑이 끝날때까지 대기한다.

        // 매치가 되었다면 블록을 삭제한 뒤 빈 블록을 채운다.
        Block[] matchBlocks = MatchBlocks(block, targetBlock);
        

        if (matchBlocks.Length >= 3)
        { 
            isLockTime = true;
            int continuously = 0;

            yield return StartCoroutine(IERemovePanelBlock(matchBlocks, continuously++));
            yield return StartCoroutine(IEMediate());

            // 이후부터는 모든 블록을 대상으로 매칭시켜본다.
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

        // 서로의 위치를 교환한다.
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

    // 블록 삭제 함수.
    IEnumerator IERemoveBlockAtferBomb(Block block)
    {
        yield return new WaitForSeconds(0.2f);

        int top = block.info.x;
        int index = top;

        // 수직으로 블록 삭제
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

        // 수평 이동.
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
        // 사운드 재생, 스코어 및 폭탄 게이지 계산.
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

    // 점수 계산.s
    void CalculateAfterBlock(int removeLength, int continuously, bool useBomb)
    {
        // 콤보 시간 갱신.
        if (comboTime > 0f)
            combo += 1;
        comboTime = MAX_COMBO_TIME;

        // 연속으로 터트릴 시 20%의 보너스, 콤보당 5%의 보너스
        int addBlock = removeLength - 3;
        float comboBonus = (1 + ((combo - 1) * 0.05f));
        float continueBonus = (1 + (continuously * 0.2f));

        // 3블록 250 > 이후 1블록 추가마다 100씩 추가, 콤보당 보너스 점수 +20%
        float addScore = (250 + (100 * addBlock)) * (comboBonus + continueBonus);
        score += Mathf.RoundToInt(addScore);

        // 폭탄 비 사용.
        if (!useBomb)
        {
            // 3블록 10 > 이후 1블록 추가마다 5씩 추가
            float addBomb = (10 + (5 * addBlock)) * (comboBonus + continueBonus);
            currentBomb += Mathf.RoundToInt(addBomb);
            if (currentBomb >= MAX_BOMB)
            {
                currentBomb %= MAX_BOMB;
                createBomb = true;
            }
        }
    }

   
    // 블록 조정하기.
    IEnumerator IEMediate()
    {
        MediateEmptySpace();        // MatchBlockToRemove()로 인해 빈 공간이 발생했을 경우 블록을 내린다.
        FillNewBlock();             // 빈 공간을 채운다.

        const float FALL_SPEED = 8f;

        // 갱신된 자신의 위치로 이동한다.
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
        // 마지막 블록부터 역순으로 자신의 아래에 빈 블록이 있다면 정보를 갱신한다.
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
        // 빈 블록에 새로운 블록을 생성한 뒤 위치를 갱신한다. (=아마 화면 밖 위쪽으로 이동)

        // 각 x축 라인별로 빈 블록의 개수를 계산한다.
        int[] emptyCounts = new int[BLOCK_WIDTH];
        for(int index = 0; index < panelBlocks.Length; index++)
        {
            if (panelBlocks[index] == null)
                emptyCounts[index % BLOCK_WIDTH] += 1;
        }

        // 모든 빈 블록을 기준으로 새로운 블록을 생성한다.
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

        // 모든 빈 인덱스를 돌면서 새로운 블록 생성.
        foreach(int index in emptyIndex)
        {
            if (panelBlocks[index] != null)
                continue;

            // 생성된 빈 블록이 속한 x축 라인의 빈 개수에 따라 위치를 올린다.
            Block newBlock = CreateNewBlock(index, index == bombIndex);
            newBlock.transform.localPosition += Vector3.up * emptyCounts[index % BLOCK_WIDTH];
            panelBlocks[index] = newBlock;
        }
    }

    // 블록 생성 및 교환하기.
    private Block CreateNewBlock(int index, bool isBomb = false)
    {
        Block prefab = isBomb ? bombBlockPrefab : blockPrefab;
        Block block = Instantiate(prefab, transform);
        block.Setup(index, index % BLOCK_WIDTH, index / BLOCK_WIDTH);

        // 블록의 위치와 크기를 설정한다.
        block.transform.localScale = new Vector3(1 / transform.localScale.x, 1 / transform.localScale.y, 1 / transform.localScale.z);
        block.transform.localPosition = IndexToLocalPos(index);
        return block;
    }
    private void SwapBlock(int indexA, int indexB)
    {
        Block blockA = panelBlocks[indexA];
        Block blockB = panelBlocks[indexB];

        // blocks 배열 내부 교환.
        panelBlocks[indexA] = blockB;
        panelBlocks[indexB] = blockA;

        // block끼리 info 교환.
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
