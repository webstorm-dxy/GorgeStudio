/*
    Gorge词法规则
*/
lexer grammar GorgeLexerRules;
    
// 关键字
Do : 'do';
New : 'new';
For : 'for';
While : 'while';
Default : 'default';
Case : 'case';
Switch : 'switch';
Else : 'else';
If : 'if';
Return : 'return';
Static : 'static';
Extends : 'extends';
Native : 'native';
Class : 'class';
Interface : 'interface';
Inject : 'inject';
Null : 'null';
Int : 'int';
Float : 'float';
Bool : 'bool';
String : 'string';
BoolLiteral : 'true' | 'false';
StringLiteral : '"' StringCharSequence? '"';
Enum : 'enum';
This : 'this';
Super : 'super';
Void : 'void';
Injector : 'injector';
Invokes : 'invokes';
Break : 'break';
Continue : 'continue';
Delegate : 'delegate';
Object : 'object';
Auto : 'auto';
Using : 'using';
Namespace : 'namespace';

// 界符
LeftParenthesis : '(';
RightParenthesis : ')';
LeftSquareBracket : '[';
RightSquareBracket : ']';
LeftCurlyBracket : '{';
RightCurlyBracket : '}';
LeftAngleBracket : '<';
RightAngleBracket : '>';

// 分割符
Comma : ',';
Colon : ':';
DoubleColon : '::';
Semicolon : ';';
Dot : '.';

// 标识
At : '@';
Caret : '^';
LambdaArrow : '->';

// 操作符
Assign : '=';
Question : '?';
LogicalOr : '||';
LogicalAnd : '&&';
Equals : '==';
Inequals : '!=';
LessEquals : '<=';
GreaterEquals : '>=';
Addition : '+';
Subtraction : '-';
Multiplication : '*';
Division : '/';
Remainder : '%';
Exclamation : '!';

// 字符串的字符序列
fragment StringCharSequence
    : StringChar+
    ;

// 字符串常量内的单个字符
fragment StringChar
    /*
        普通字符，除了
        引号 : 字串边界指示
        斜杠 : 转义引导
        回车换行
    */
    : ~["\\\r\n]
    | EscapeSequence // 转义序列认为是单个字符
    ;

// 整数字面量，非零打头的数字串或0
// 符号可能应当认为是表达式的一部分，赋值位置应当逐渐调整成表达式
// 123
// 0
IntLiteral
    : NonzeroDigit Digit*
    | '0'
    ;

// 转义字串
fragment EscapeSequence
    /*
        由\引导
        \" = "
        \\ = \
        \n = \n
        \r = \r
        由于暂时不考虑支持char基本类型，所以单引号目前不需要转义
        暂时不支持制表符，似乎不用ASCII就可以直接使用
    */
    : '\\' ["nr\\]
    ;

// 标识符，字母打头的字母数字串
Identifier
    : Nondigit (Nondigit | Digit)*
    ;

// 标识符中字母，含下划线
fragment Nondigit
    : [a-zA-Z_]
    ;

// 非0数字
fragment NonzeroDigit
    : [1-9]
    ;

// 数字
fragment Digit
    : [0-9]
    ;

// 浮点字面量，整数部分是一个整型常量，小数部分是一个任意数字串
// 123.45
// 0.01
FloatLiteral
    : IntLiteral '.' Digit+
    ;

// 默认忽略空格、换行、注释
Whitespace
    : [ \t]+ -> channel(HIDDEN)
    ;

BlockComment
    : '/*' .*? '*/' ->  channel(HIDDEN)
    ;
    
LineComment
    : '//' ~[\r\n]* -> channel(HIDDEN)
    ;

Newline
    : ('\r' '\n'? | '\n') -> channel(HIDDEN)
    ;