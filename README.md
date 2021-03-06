# LockstepPlatform 
- A lockstep solution base on ECS Framework,Include a demo FCTank 
- 基于ECS 可回滚 帧同步解决方案，含demo  FC Tank 


<p align="center"> <img src="https://github.com/JiepengTan/JiepengTan.github.io/blob/master/assets/img/blog/Show/game_pic.png?raw=true" width="512"/></p>



<p align="center"><img src="https://github.com/JiepengTan/JiepengTan.github.io/blob/master/assets/img/blog/Show/lockstepgifbig.gif?raw=true" width="512"></p>

## **Done**
    - Ring network frame buffer
    - ECS rollback
    - IService rollback
    - IService autoregister
    - NetworkMsg auto parse and register
    - Event auto register
    - Pursue frame after reconnected


## **TODO**
    - changed Server API to PUN API
    - using UDP for Lockstep frame msg


## **References：** 
- Inspired by UnityLockstep:[https://github.com/proepkes/UnityLockstep][1] 
- Network using LiteNetLib: [https://github.com/RevenantX/LiteNetLib][2] 
- Uses Entitas as ECS Framework: [https://github.com/sschmid/Entitas-CSharp][3] 
- Deterministic Math lib: [https://github.com/JiepengTan/LockstepMath][4] 
- Deterministic Collision2D lib  : [https://github.com/JiepengTan/LockstepCollision2D][5] 


 [1]: https://github.com/proepkes/UnityLockstep
 [2]: https://github.com/RevenantX/LiteNetLib
 [3]: https://github.com/sschmid/Entitas-CSharp
 [4]: https://github.com/JiepengTan/LockstepMath
 [5]: https://github.com/JiepengTan/LockstepCollision2D
