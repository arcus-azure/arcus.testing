import React from 'react';
import DefaultAdmonitionTypes from '@theme-original/Admonition/Types';

function PraiseAdmonition(props) {
  props.title = props.title === "" ? "Praise" : props.title;
  return (
    <div class="theme-admonition alert alert--praise">
      <div>
        <span><i class="fa-solid fa-award"></i> {props.title}</span>
      </div>
      <div>{props.children}</div>
    </div>
  );
}

const AdmonitionTypes = {
  ...DefaultAdmonitionTypes,

  // Add all your custom admonition types here...
  // You can also override the default ones if you want
  'praise': PraiseAdmonition,
};

export default AdmonitionTypes;