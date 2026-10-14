export interface TopButton {
    label: string;
    bgColor?: string;
    hoverColor?: string;
    loading?: boolean;
    showCondition?: boolean;
    enableCondition?: boolean;
    action: FunctionConstructor;
}