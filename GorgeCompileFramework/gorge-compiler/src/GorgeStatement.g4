/*
    Gorge语句语法
*/
grammar GorgeStatement;
import GorgeLexerRules, GorgeExpression;

/*
    代码块序列
*/
codeBlockList
    : codeBlock+
    ;
    
/*
    代码块
    暂时认为For等结构是一种特殊的代码块，而不是特殊语句+普通代码块
    因为for括号内声明的变量是属于For代码块内部的，多个平行的For上可以声明相同的变量
*/
codeBlock
    : (Else)? LeftCurlyBracket semicolonStatementList RightCurlyBracket                                          # NormalBlock
    | (Else)? If LeftParenthesis expression RightParenthesis LeftCurlyBracket semicolonStatementList RightCurlyBracket                    # IfBlock
    | (Else)? Switch LeftParenthesis expression RightParenthesis LeftCurlyBracket switchCase+ RightCurlyBracket                           # SwitchBlock
    | (Else)? While LeftParenthesis expression RightParenthesis LeftCurlyBracket semicolonStatementList  RightCurlyBracket                # WhileBlock
    | (Else)? Do LeftCurlyBracket semicolonStatementList  RightCurlyBracket While LeftParenthesis expression RightParenthesis ';'         # DoWhileBlock
    | (Else)? For LeftParenthesis commaStatementList ';' expression ';' commaStatementList  RightParenthesis 
                LeftCurlyBracket semicolonStatementList RightCurlyBracket                                          # ForBlock
    ;
    
/*
    switch条件块
      不是代码块，和平行的条件块共享同一个代码块上下文
*/
switchCase
    : switchCaseCondition semicolonStatementList
    ;
    
/*
    switch条件标签
*/
switchCaseCondition
    : Case expression ':'   # NormalCase
    | Default ':'           # DefaultCase
    ;
    
/*
    逗号语句序列
*/
commaStatementList
    : (commaStatement (Comma commaStatement)*)?
    ;
    
/*
    逗号语句，for中使用:
        语句内容
        代码块序列
        不允许空语句
*/
commaStatement
    : statementContent
    | codeBlockList
    ;
    
/*
    分号语句序列
*/
semicolonStatementList
    : semicolonStatement*
    ;
    
/*
    分号语句:
      语句内容 加分号
      代码块序列 不加分号
      空语句
*/
semicolonStatement
    : statementContent ';'  # NormalStatement
    | codeBlockList         # BlockListStatement
    | ';'                   # EmptyStatement
    ;
    
/*
    语句内容，不带分号
*/
statementContent 
    : localVariableDeclaration      # LocalVariableDeclarationStatement // 这里本地变量定义一定要放在表达式前，否则泛型尖括号会被识别为比较表达式
    | expression                    # ExpressionStatement
    | Return expression?            # ReturnStatement
    | Break leaveBlockTarget*       # BreakStatement
    | Continue leaveBlockTarget*    # ContinueStatement
    ;
    
/*
    离块语句目标
*/
leaveBlockTarget
    : IntLiteral    # LeaveSpecificQuantity
    | For           # LeaveFor
    | While         # LeaveWhile
    | Switch        # LeaveSwitch
    | Else          # LeaveElse
    | If            # LeaveIf
    | Do            # LeaveDoWhile
    ;

// 本地变量声明语句，暂时不允许声明时赋值
localVariableDeclaration
    : expression Identifier ('=' expression)?
    ;