// DSH client 入口（P1 + P0 全局渲染）
//   1) 注册 tool.call.toolview keyed 卡片（key = cjdsl_render）——工具结果通道；
//   2) 注册 conversationEvents Definition（kind=cjdsl）+ conversation.chat.node
//      keyed 渲染器（key = cjdsl）——全局文本 DSL 载荷通道（P0）。
import React from "react";
import { CjdslToolCard, ChatDslNode, detectDslPayload, detectDslPayloadInText } from "@cj/cjdsl-react";
import { defaultApiClient } from "./api";

export const inject = ["slots", "locale", "conversationEvents"];

const cjdslPayloadDefinition = {
  kind: "cjdsl",
  target: "chat",
  match: (event: any) => {
    if (!event) return null;
    const isAssistant = event.type === "assistant/message";
    const isPluginInjected = event.type === "user/message" && event.data?.source?.kind === "plugin";
    if (!isAssistant && !isPluginInjected) return null;
    const content = event.data?.message?.content ?? event.data?.content;
    if (!Array.isArray(content)) return null;
    if (!detectDslPayload(content)) return null;
    const id = `${event.data?.turn ?? 0}:${event.data?.step ?? 0}:${String(event.data?.message?.id ?? event.seq)}`;
    return { id, role: "start" };
  },
  start: (_context: any,  match: any) => {
    const content = match.event?.data?.message?.content ?? match.event?.data?.content;
    const rawText = Array.isArray(content)
      ? content
          .map((b: any) =>
            typeof b?.text === "string"
              ? b.text
              : typeof b?.content === "string"
                ? b.content
                : typeof b === "string"
                  ? b
                  : "",
          )
          .filter(Boolean)
          .join("\n")
      : typeof content === "string"
        ? content
        : "";
    const det = detectDslPayload(content) ?? (rawText ? detectDslPayloadInText(rawText) : null);
    if (!det) return { payload: null, dsl: null, mode: "card", rawText };
    return { payload: det.payload, dsl: det.dsl, mode: det.mode, rawText };
  },
  update: (context: any) => context.state,
  buildViewNode: (context: any) => {
    if (context.state === undefined) return null;
    return {
      key: context.key,
      kind: "cjdsl",
      id: context.id,
      target: "chat",
      anchorSeq: context.start?.event?.seq ?? context.matches?.[0]?.event?.seq ?? 0,
      location: context.start?.location ?? { kind: "unresolved" },
      visibility: "visible",
      data: context.state,
    };
  },
};

export function apply(ctx: any): void {
  console.log("[cjdsl-react] apply entered");
  try {
    const slots = ctx.slots;
    if (!slots) {
      console.log("[cjdsl-react] slots unavailable, skip");
      return;
    }

    slots.inject("tool.call.toolview", () =>
      slots.register(
        { name: "tool.call.toolview", key: "cjdsl_render", id: "cjdsl_render", label: "CJDSL" },
        CjdslToolCard,
      ),
    );

    slots.inject("conversation.chat.node", () =>
      slots.register(
        { name: "conversation.chat.node", key: "cjdsl", id: "cjdsl", label: "CJDSL" },
        (props: any) => <ChatDslNode {...props} api={defaultApiClient} />,
      ),
    );

    const ce = ctx.conversationEvents;
    if (ce && typeof ce.register === "function") {
      ce.register(cjdslPayloadDefinition);
      console.log("[cjdsl-react] conversationEvents Definition registered (kind=cjdsl)");
    } else {
      console.log("[cjdsl-react] conversationEvents unavailable, global DSL node skipped");
    }
  } catch (e) {
    console.log(`[cjdsl-react] apply failed: ${(e as Error).message}`);
  }
}
